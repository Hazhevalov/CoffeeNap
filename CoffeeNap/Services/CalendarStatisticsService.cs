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
    private readonly object _cacheGate = new();
    private readonly Dictionary<MonthKey, MonthCacheEntry> _monthCache = [];
    private readonly Dictionary<MonthKey, Task<CalendarMonthStatistics>> _monthLoads = [];
    private readonly Dictionary<MonthKey, int> _monthGenerations = [];
    private readonly Dictionary<DateTime, Task<CalendarWeekStatistics>> _weekLoads = [];
    private CalendarWeekStatistics? _weekCache;
    private int _weekGeneration;
    private int _dataGeneration;
    private long _accessSequence;

    public CalendarStatisticsService(
        IAppDataService dataService,
        ILogger<CalendarStatisticsService> logger)
    {
        _dataService = dataService;
        _logger = logger;
        _dataService.ConsumptionAdded += OnConsumptionAdded;
        _dataService.ConsumptionDeleted += OnConsumptionDeleted;
        _dataService.UserDataDeleted += OnUserDataDeleted;
    }

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

        if (!hasMonth && !hasWeek)
        {
            return await LoadCombinedAsync(month, weekStart, cancellationToken)
                .ConfigureAwait(false);
        }

        var monthResult = hasMonth
            ? cachedMonth!
            : await GetMonthAsync(month, cancellationToken).ConfigureAwait(false);
        var weekResult = hasWeek
            ? cachedWeek!
            : await GetCurrentWeekAsync(today, cancellationToken).ConfigureAwait(false);
        return new CalendarInitialStatistics(monthResult, weekResult);
    }

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

    public void PrefetchAdjacentMonths(DateTime month)
    {
        month = FirstOfMonth(month);
        _ = ObservePrefetchAsync(month.AddMonths(-1));
        _ = ObservePrefetchAsync(month.AddMonths(1));
    }

    private async Task<CalendarInitialStatistics> LoadCombinedAsync(
        DateTime month,
        DateTime weekStart,
        CancellationToken cancellationToken)
    {
        var monthEnd = month.AddMonths(1);
        var weekEnd = weekStart.AddDays(7);
        var rangeStart = month < weekStart ? month : weekStart;
        var rangeEnd = monthEnd > weekEnd ? monthEnd : weekEnd;
        var key = MonthKey.From(month);
        while (true)
        {
            int monthGeneration;
            int weekGeneration;
            int dataGeneration;
            lock (_cacheGate)
            {
                monthGeneration = GetMonthGeneration(key);
                weekGeneration = _weekGeneration;
                dataGeneration = _dataGeneration;
            }

            var watch = Stopwatch.StartNew();
            var consumptions = await _dataService.GetConsumptionsBetweenAsync(
                    ToUtcBoundary(rangeStart),
                    ToUtcBoundary(rangeEnd))
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var queryElapsed = watch.Elapsed.TotalMilliseconds;
            var statistics = Aggregate(consumptions);
            var monthResult = BuildMonthStatistics(
                month,
                Slice(statistics, month, monthEnd));
            var weekResult = new CalendarWeekStatistics(
                weekStart,
                Slice(statistics, weekStart, weekEnd));

            lock (_cacheGate)
            {
                if (monthGeneration != GetMonthGeneration(key) ||
                    weekGeneration != _weekGeneration ||
                    dataGeneration != _dataGeneration)
                {
                    continue;
                }

                StoreMonth(key, monthResult);
                _weekCache = weekResult;
            }

            _logger.LogDebug(
                "Calendar initial range loaded: query {QueryMs:F1} ms, total {TotalMs:F1} ms, {RowCount} rows.",
                queryElapsed,
                watch.Elapsed.TotalMilliseconds,
                consumptions.Count);
            return new CalendarInitialStatistics(monthResult, weekResult);
        }
    }

    private async Task<CalendarMonthStatistics> LoadMonthAndCacheAsync(
        DateTime month,
        MonthKey key)
    {
        try
        {
            while (true)
            {
                int monthGeneration;
                int dataGeneration;
                lock (_cacheGate)
                {
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
                _monthLoads.Remove(key);
            }
        }
    }

    private async Task<CalendarWeekStatistics> LoadWeekAndCacheAsync(DateTime weekStart)
    {
        try
        {
            while (true)
            {
                int weekGeneration;
                int dataGeneration;
                lock (_cacheGate)
                {
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
                _weekLoads.Remove(weekStart);
            }
        }
    }

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

    private bool TryGetMonth(DateTime month, out CalendarMonthStatistics? result)
    {
        var key = MonthKey.From(month);
        lock (_cacheGate)
        {
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

    private bool TryGetWeek(DateTime weekStart, out CalendarWeekStatistics? result)
    {
        lock (_cacheGate)
        {
            if (_weekCache?.WeekStart == weekStart)
            {
                result = _weekCache;
                return true;
            }
        }

        result = null;
        return false;
    }

    private void StoreMonth(MonthKey key, CalendarMonthStatistics data)
    {
        _monthCache[key] = new MonthCacheEntry(data, ++_accessSequence);
        while (_monthCache.Count > MaximumCachedMonths)
        {
            var oldest = _monthCache.MinBy(pair => pair.Value.LastAccess).Key;
            _monthCache.Remove(oldest);
        }
    }

    private void OnConsumptionAdded(object? sender, CaffeineConsumption consumption) =>
        InvalidateConsumptionDate(consumption);

    private void OnConsumptionDeleted(object? sender, CaffeineConsumption consumption) =>
        InvalidateConsumptionDate(consumption);

    private void InvalidateConsumptionDate(CaffeineConsumption consumption)
    {
        var localDate = consumption.ConsumedAt.ToLocalTime().Date;
        var monthKey = MonthKey.From(localDate);
        var currentWeekStart = GetWeekStart(DateTime.Today);
        lock (_cacheGate)
        {
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

    private void OnUserDataDeleted(object? sender, EventArgs eventArgs)
    {
        lock (_cacheGate)
        {
            _monthCache.Clear();
            _weekCache = null;
            _dataGeneration++;
        }
    }

    private int GetMonthGeneration(MonthKey key) =>
        _monthGenerations.GetValueOrDefault(key);

    private static IReadOnlyDictionary<DateOnly, CalendarDailyStatistics> Aggregate(
        IReadOnlyList<CaffeineConsumption> consumptions)
    {
        var builders = new Dictionary<DateOnly, DailyStatisticsBuilder>();
        foreach (var consumption in consumptions)
        {
            var localDate = DateOnly.FromDateTime(consumption.ConsumedAt.ToLocalTime().Date);
            if (!builders.TryGetValue(localDate, out var statistics))
            {
                statistics = new DailyStatisticsBuilder();
                builders.Add(localDate, statistics);
            }

            statistics.Add(consumption);
        }

        return builders.ToDictionary(pair => pair.Key, pair => pair.Value.Build());
    }

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

    private static IReadOnlyDictionary<DateOnly, CalendarDailyStatistics> Slice(
        IReadOnlyDictionary<DateOnly, CalendarDailyStatistics> source,
        DateTime fromInclusive,
        DateTime toExclusive)
    {
        var from = DateOnly.FromDateTime(fromInclusive);
        var to = DateOnly.FromDateTime(toExclusive);
        return source
            .Where(pair => pair.Key >= from && pair.Key < to)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static DateTime FirstOfMonth(DateTime value) =>
        new(value.Year, value.Month, 1);

    private static DateTime GetWeekStart(DateTime date)
    {
        var mondayOffset = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-mondayOffset);
    }

    private static DateTimeOffset ToUtcBoundary(DateTime localDate)
    {
        var unspecified = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
        return new DateTimeOffset(
                unspecified,
                TimeZoneInfo.Local.GetUtcOffset(unspecified))
            .ToUniversalTime();
    }

    private readonly record struct MonthKey(int Year, int Month)
    {
        public static MonthKey From(DateTime value) => new(value.Year, value.Month);
    }

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

        public CalendarDailyStatistics Build() => new(
            _totalCaffeineMg,
            new ConsumptionTypeDistribution(
                _coffeeCount,
                _teaCount,
                _energyDrinkCount));
    }
}
