using System.Diagnostics;
using CoffeeNap.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.Services;

/// <summary>
/// Loads calendar ranges through the shared data layer, aggregates each result
/// in one pass, and keeps a small process-only cache.
/// </summary>
public sealed class CalendarStatisticsService
{
    private const int MaximumCachedMonths = 3;

    private readonly IAppDataService _dataService;
    private readonly ILogger<CalendarStatisticsService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly object _cacheGate = new();
    private readonly Dictionary<MonthKey, MonthCacheEntry> _monthCache = [];
    private readonly Dictionary<MonthKey, Task<CalendarMonthStatistics>> _monthLoads = [];
    private readonly Dictionary<MonthKey, int> _monthGenerations = [];
    private readonly Dictionary<DateTime, Task<CalendarWeekStatistics>> _weekLoads = [];
    private CalendarWeekStatistics? _weekCache;
    private int _weekGeneration;
    private int _dataGeneration;
    private long _accessSequence;
    private string _zoneKey;

    // Initializes the calendar statistics service.
    public CalendarStatisticsService(
        IAppDataService dataService,
        ILogger<CalendarStatisticsService> logger,
        TimeProvider? timeProvider = null)
    {
        _dataService = dataService;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _zoneKey = _timeProvider.LocalTimeZone.ToSerializedString();
        _dataService.ConsumptionAdded += OnConsumptionAdded;
        _dataService.ConsumptionDeleted += OnConsumptionDeleted;
        _dataService.UserDataDeleted += OnUserDataDeleted;
    }

    // Loads the initial month and current week statistics.
    public async Task<CalendarInitialStatistics> GetInitialAsync(
        DateTime month,
        DateTime today,
        CancellationToken cancellationToken = default)
    {
        month = FirstOfMonth(month);
        var weekStart = GetWeekStart(today);

        var hasMonth = TryGetMonth(month, out var cachedMonth);
        var hasWeek = TryGetWeek(weekStart, out var cachedWeek);
        if (hasMonth && hasWeek)
        {
            return new CalendarInitialStatistics(cachedMonth!, cachedWeek!);
        }

        var monthTask = hasMonth ? Task.FromResult(cachedMonth!) : GetMonthAsync(month, cancellationToken);
        var weekTask = hasWeek ? Task.FromResult(cachedWeek!) : GetCurrentWeekAsync(today, cancellationToken);
        await Task.WhenAll(monthTask, weekTask).ConfigureAwait(false);
        var monthResult = await monthTask.ConfigureAwait(false);
        var weekResult = await weekTask.ConfigureAwait(false);
        return new CalendarInitialStatistics(monthResult, weekResult);
    }

    // Returns cached month statistics or loads them.
    public Task<CalendarMonthStatistics> GetMonthAsync(
        DateTime month,
        CancellationToken cancellationToken = default)
    {
        month = FirstOfMonth(month);
        if (TryGetMonth(month, out var cached))
        {
            return Task.FromResult(cached!);
        }

        var key = MonthKey.From(month);
        Task<CalendarMonthStatistics> load;
        lock (_cacheGate)
        {
            EnsureTimeZone();
            if (!_monthLoads.TryGetValue(key, out load!))
            {
                load = LoadMonthAndCacheAsync(month, key);
                _monthLoads.Add(key, load);
            }
        }

        return cancellationToken.CanBeCanceled
            ? load.WaitAsync(cancellationToken)
            : load;
    }

    // Returns cached current-week statistics or loads them.
    public Task<CalendarWeekStatistics> GetCurrentWeekAsync(
        DateTime today,
        CancellationToken cancellationToken = default)
    {
        var weekStart = GetWeekStart(today);
        if (TryGetWeek(weekStart, out var cached))
        {
            return Task.FromResult(cached!);
        }

        Task<CalendarWeekStatistics> load;
        lock (_cacheGate)
        {
            EnsureTimeZone();
            if (!_weekLoads.TryGetValue(weekStart, out load!))
            {
                load = LoadWeekAndCacheAsync(weekStart);
                _weekLoads.Add(weekStart, load);
            }
        }

        return cancellationToken.CanBeCanceled
            ? load.WaitAsync(cancellationToken)
            : load;
    }

    // Starts preloading the months adjacent to the selected month.
    public void PrefetchAdjacentMonths(DateTime month)
    {
        month = FirstOfMonth(month);
        _ = ObservePrefetchAsync(month.AddMonths(-1));
        _ = ObservePrefetchAsync(month.AddMonths(1));
    }

    // Loads and caches month statistics if they are still current.
    private async Task<CalendarMonthStatistics> LoadMonthAndCacheAsync(
        DateTime month,
        MonthKey key)
    {
        await Task.Yield(); // Register the in-flight task before it can complete.
        try
        {
            while (true)
            {
                int monthGeneration;
                int dataGeneration;
                lock (_cacheGate)
                {
                    EnsureTimeZone();
                    monthGeneration = GetMonthGeneration(key);
                    dataGeneration = _dataGeneration;
                }

                var monthEnd = month.AddMonths(1);
                var watch = Stopwatch.StartNew();
                var consumptions = await _dataService.GetConsumptionsBetweenAsync(
                        ToUtcBoundary(month),
                        ToUtcBoundary(monthEnd))
                    .ConfigureAwait(false);
                var queryElapsed = watch.Elapsed.TotalMilliseconds;
                var result = BuildMonthStatistics(month, Aggregate(consumptions));
                lock (_cacheGate)
                {
                    EnsureTimeZone();
                    if (monthGeneration != GetMonthGeneration(key) ||
                        dataGeneration != _dataGeneration)
                    {
                        continue;
                    }

                    StoreMonth(key, result);
                }

                _logger.LogDebug(
                    "Calendar month {Month:yyyy-MM} loaded: query {QueryMs:F1} ms, total {TotalMs:F1} ms, {RowCount} rows.",
                    month,
                    queryElapsed,
                    watch.Elapsed.TotalMilliseconds,
                    consumptions.Count);
                return result;
            }
        }
        finally
        {
            lock (_cacheGate)
            {
                EnsureTimeZone();
                _monthLoads.Remove(key);
            }
        }
    }

    // Loads and caches week statistics if they are still current.
    private async Task<CalendarWeekStatistics> LoadWeekAndCacheAsync(DateTime weekStart)
    {
        await Task.Yield();
        try
        {
            while (true)
            {
                int weekGeneration;
                int dataGeneration;
                lock (_cacheGate)
                {
                    EnsureTimeZone();
                    weekGeneration = _weekGeneration;
                    dataGeneration = _dataGeneration;
                }

                var weekEnd = weekStart.AddDays(7);
                var watch = Stopwatch.StartNew();
                var consumptions = await _dataService.GetConsumptionsBetweenAsync(
                        ToUtcBoundary(weekStart),
                        ToUtcBoundary(weekEnd))
                    .ConfigureAwait(false);
                var result = new CalendarWeekStatistics(weekStart, Aggregate(consumptions));
                lock (_cacheGate)
                {
                    EnsureTimeZone();
                    if (weekGeneration != _weekGeneration || dataGeneration != _dataGeneration)
                    {
                        continue;
                    }

                    _weekCache = result;
                }

                _logger.LogDebug(
                    "Calendar week {WeekStart:yyyy-MM-dd} loaded in {TotalMs:F1} ms ({RowCount} rows).",
                    weekStart,
                    watch.Elapsed.TotalMilliseconds,
                    consumptions.Count);
                return result;
            }
        }
        finally
        {
            lock (_cacheGate)
            {
                EnsureTimeZone();
                _weekLoads.Remove(weekStart);
            }
        }
    }

    // Observes a prefetch task and logs failures.
    private async Task ObservePrefetchAsync(DateTime month)
    {
        try
        {
            await GetMonthAsync(month).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Calendar prefetch failed for {Month:yyyy-MM}.", month);
        }
    }

    // Tries to retrieve valid cached month statistics.
    private bool TryGetMonth(DateTime month, out CalendarMonthStatistics? result)
    {
        var key = MonthKey.From(month);
        lock (_cacheGate)
        {
            EnsureTimeZone();
            if (_monthCache.TryGetValue(key, out var entry))
            {
                entry.LastAccess = ++_accessSequence;
                result = entry.Data;
                return true;
            }
        }

        result = null;
        return false;
    }

    // Tries to retrieve valid cached week statistics.
    private bool TryGetWeek(DateTime weekStart, out CalendarWeekStatistics? result)
    {
        lock (_cacheGate)
        {
            EnsureTimeZone();
            if (_weekCache?.WeekStart == weekStart)
            {
                result = _weekCache;
                return true;
            }
        }

        result = null;
        return false;
    }

    // Caches month statistics and maintains the cache size limit.
    private void StoreMonth(MonthKey key, CalendarMonthStatistics data)
    {
        _monthCache[key] = new MonthCacheEntry(data, ++_accessSequence);
        while (_monthCache.Count > MaximumCachedMonths)
        {
            var oldest = _monthCache.MinBy(pair => pair.Value.LastAccess).Key;
            _monthCache.Remove(oldest);
        }
    }

    // Invalidates statistics affected by a consumption change.
    private void OnConsumptionAdded(object? sender, CaffeineConsumption consumption) =>
        InvalidateConsumptionDate(consumption);

    // Invalidates statistics affected by a consumption change.
    private void OnConsumptionDeleted(object? sender, CaffeineConsumption consumption) =>
        InvalidateConsumptionDate(consumption);

    // Invalidates cached statistics for the consumption's local date.
    private void InvalidateConsumptionDate(CaffeineConsumption consumption)
    {
        var localDate = TimeZoneInfo.ConvertTime(consumption.ConsumedAt, _timeProvider.LocalTimeZone).Date;
        var monthKey = MonthKey.From(localDate);
        var currentWeekStart = GetWeekStart(_timeProvider.GetLocalNow().Date);
        lock (_cacheGate)
        {
            EnsureTimeZone();
            _monthCache.Remove(monthKey);
            _monthGenerations[monthKey] = GetMonthGeneration(monthKey) + 1;
            if (localDate >= currentWeekStart &&
                localDate < currentWeekStart.AddDays(7))
            {
                _weekCache = null;
                _weekGeneration++;
            }
        }
    }

    // Clears cached statistics after user data is deleted.
    private void OnUserDataDeleted(object? sender, EventArgs eventArgs)
    {
        lock (_cacheGate)
        {
            EnsureTimeZone();
            _monthCache.Clear();
            _weekCache = null;
            _dataGeneration++;
        }
    }

    // Called only while holding _cacheGate. In-flight loads retry using the new generation.
    private void EnsureTimeZone()
    {
        var current = _timeProvider.LocalTimeZone.ToSerializedString();
        if (_zoneKey == current) return;
        _zoneKey = current;
        _monthCache.Clear();
        _weekCache = null;
        _dataGeneration++;
    }

    // Returns the cache generation for a month.
    private int GetMonthGeneration(MonthKey key) =>
        _monthGenerations.GetValueOrDefault(key);

    // Groups consumptions into statistics by local calendar date.
    private IReadOnlyDictionary<DateOnly, CalendarDailyStatistics> Aggregate(
        IReadOnlyList<CaffeineConsumption> consumptions)
    {
        var builders = new Dictionary<DateOnly, DailyStatisticsBuilder>();
        foreach (var consumption in consumptions)
        {
            var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(consumption.ConsumedAt, _timeProvider.LocalTimeZone).Date);
            if (!builders.TryGetValue(localDate, out var statistics))
            {
                statistics = new DailyStatisticsBuilder();
                builders.Add(localDate, statistics);
            }

            statistics.Add(consumption);
        }

        return builders.ToDictionary(pair => pair.Key, pair => pair.Value.Build());
    }

    // Builds month totals and highlights from daily statistics.
    private static CalendarMonthStatistics BuildMonthStatistics(
        DateTime month,
        IReadOnlyDictionary<DateOnly, CalendarDailyStatistics> days)
    {
        var monthEnd = month.AddMonths(1);
        var bestComboDays = 0;
        var currentComboDays = 0;
        var mostConsumedInDayMg = 0;

        for (var date = month; date < monthEnd; date = date.AddDays(1))
        {
            if (days.TryGetValue(DateOnly.FromDateTime(date), out var daily))
            {
                mostConsumedInDayMg = Math.Max(mostConsumedInDayMg, daily.TotalCaffeineMg);
                if (daily.Distribution.TotalCount > 0)
                {
                    currentComboDays++;
                    bestComboDays = Math.Max(bestComboDays, currentComboDays);
                    continue;
                }
            }

            currentComboDays = 0;
        }

        return new CalendarMonthStatistics(
            month,
            days,
            bestComboDays,
            mostConsumedInDayMg);
    }

    // Returns the first day of the given month.
    private static DateTime FirstOfMonth(DateTime value) =>
        new(value.Year, value.Month, 1);

    // Returns the Monday that starts the given date's week.
    private static DateTime GetWeekStart(DateTime date)
    {
        var mondayOffset = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-mondayOffset);
    }

    // Converts a local date boundary to UTC.
    private DateTimeOffset ToUtcBoundary(DateTime localDate)
    {
        var unspecified = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
        return new DateTimeOffset(
                unspecified,
                _timeProvider.LocalTimeZone.GetUtcOffset(unspecified))
            .ToUniversalTime();
    }

    // Identifies a calendar month by year and month number.
    private readonly record struct MonthKey(int Year, int Month)
    {
        // Creates a cache key from the date's year and month.
        public static MonthKey From(DateTime value) => new(value.Year, value.Month);
    }

    // Stores cached month statistics and their last access marker.
    private sealed class MonthCacheEntry(
        CalendarMonthStatistics data,
        long lastAccess)
    {
        public CalendarMonthStatistics Data { get; } = data;
        public long LastAccess { get; set; } = lastAccess;
    }

    private sealed class DailyStatisticsBuilder
    {
        private int _coffeeCount;
        private int _teaCount;
        private int _energyDrinkCount;
        private int _totalCaffeineMg;

        // Adds a consumption to the daily statistics totals.
        public void Add(CaffeineConsumption consumption)
        {
            _totalCaffeineMg = (int)Math.Min(
                int.MaxValue,
                (long)_totalCaffeineMg + Math.Max(0, consumption.CaffeineMg));
            switch (consumption.Type)
            {
                case CaffeineConsumptionType.Coffee:
                    _coffeeCount++;
                    break;
                case CaffeineConsumptionType.Tea:
                    _teaCount++;
                    break;
                case CaffeineConsumptionType.EnergyDrink:
                    _energyDrinkCount++;
                    break;
            }
        }

        // Creates daily statistics from the accumulated totals.
        public CalendarDailyStatistics Build() => new(
            _totalCaffeineMg,
            new ConsumptionTypeDistribution(
                _coffeeCount,
                _teaCount,
                _energyDrinkCount));
    }
}
