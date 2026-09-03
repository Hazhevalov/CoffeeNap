using System.Diagnostics;
using CoffeeNap.Models;
using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.ViewModels;

public partial class CalendarPageViewModel : ObservableObject
{
    private const double CalendarRowHeight = 46;
    private static readonly TimeSpan DateChangeCheckInterval = TimeSpan.FromMinutes(1);

    private readonly IAppDataService _dataService;
    private readonly LocalizationService _localization;
    private readonly ILogger<CalendarPageViewModel> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IReadOnlyList<CalendarDayItem> _days = [];
    private IReadOnlyList<WeeklyConsumptionItem> _weeklyDays = [];
    private DateTime _displayedMonth;
    private string _monthTitle = string.Empty;
    private string? _loadError;
    private bool _isLoading;
    private int _refreshVersion;
    private CancellationTokenSource? _dateMonitorCancellation;
    private DateTime _lastObservedDate = DateTime.Today;

    public CalendarPageViewModel(
        IAppDataService dataService,
        LocalizationService localization,
        ILogger<CalendarPageViewModel> logger,
        MainHeaderViewModel header,
        BottomNavigationViewModel navigation)
    {
        _dataService = dataService;
        _localization = localization;
        _logger = logger;
        Header = header;
        Navigation = navigation;
        Navigation.ActiveTab = NavigationTab.Calendar;

        var today = DateTime.Today;
        _displayedMonth = new DateTime(today.Year, today.Month, 1);
        UpdateLocalizedText();
        PublishPresentation(BuildDailyStatistics([]), today);
        _localization.CultureChanged += OnCultureChanged;
    }

    public MainHeaderViewModel Header { get; }

    public BottomNavigationViewModel Navigation { get; }

    public IReadOnlyList<CalendarDayItem> Days
    {
        get => _days;
        private set
        {
            if (SetProperty(ref _days, value))
            {
                OnPropertyChanged(nameof(CalendarHeight));
            }
        }
    }

    public IReadOnlyList<WeeklyConsumptionItem> WeeklyDays
    {
        get => _weeklyDays;
        private set => SetProperty(ref _weeklyDays, value);
    }

    public string MonthTitle
    {
        get => _monthTitle;
        private set => SetProperty(ref _monthTitle, value);
    }

    public string LowRangeLabel =>
        $"1–{CaffeineLevelResolver.MediumMinimumMg - 1}{_localization["MilligramShort"]}";

    public string MediumRangeLabel =>
        $"{CaffeineLevelResolver.MediumMinimumMg}–{CaffeineLevelResolver.HighMinimumMg - 1}{_localization["MilligramShort"]}";

    public string HighRangeLabel =>
        $"{CaffeineLevelResolver.HighMinimumMg}–{CaffeineLevelResolver.LimitExceededMinimumMg - 1}{_localization["MilligramShort"]}";

    public string ExceededRangeLabel =>
        $"{CaffeineLevelResolver.LimitExceededMinimumMg}+{_localization["MilligramShort"]}";

    public double CalendarHeight => Math.Ceiling(Days.Count / 7d) * CalendarRowHeight;

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string? LoadError
    {
        get => _loadError;
        private set
        {
            if (SetProperty(ref _loadError, value))
            {
                OnPropertyChanged(nameof(HasLoadError));
            }
        }
    }

    public bool HasLoadError => !string.IsNullOrEmpty(LoadError);

    public async Task RefreshAsync()
    {
        var requestVersion = Interlocked.Increment(ref _refreshVersion);
        await _refreshLock.WaitAsync();
        try
        {
            if (requestVersion != Volatile.Read(ref _refreshVersion))
            {
                return;
            }

            await SetLoadingStateAsync(true, null);
            var stopwatch = Stopwatch.StartNew();
            var today = DateTime.Today;
            var monthStart = _displayedMonth;
            var monthEnd = monthStart.AddMonths(1);
            var weekStart = GetWeekStart(today);
            var weekEnd = weekStart.AddDays(7);
            var rangeStart = monthStart < weekStart ? monthStart : weekStart;
            var rangeEnd = monthEnd > weekEnd ? monthEnd : weekEnd;

            var consumptions = await _dataService.GetConsumptionsBetweenAsync(
                ToUtcBoundary(rangeStart),
                ToUtcBoundary(rangeEnd));
            var queryElapsed = stopwatch.Elapsed;
            var statistics = BuildDailyStatistics(consumptions);

            if (requestVersion != Volatile.Read(ref _refreshVersion))
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                PublishPresentation(statistics, today);
                LoadError = null;
            });

            stopwatch.Stop();
            _logger.LogDebug(
                "Calendar refreshed with one range query in {QueryMs} ms; total {TotalMs} ms ({Count} rows).",
                queryElapsed.TotalMilliseconds,
                stopwatch.Elapsed.TotalMilliseconds,
                consumptions.Count);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Calendar refresh failed.");
            await SetLoadingStateAsync(false, _localization["LoadDataFailed"]);
        }
        finally
        {
            await SetLoadingStateAsync(false, LoadError);
            _refreshLock.Release();
        }
    }

    public void StartDateChangeMonitor()
    {
        if (_dateMonitorCancellation is { IsCancellationRequested: false })
        {
            return;
        }

        _lastObservedDate = DateTime.Today;
        _dateMonitorCancellation = new CancellationTokenSource();
        _ = MonitorDateChangeAsync(_dateMonitorCancellation.Token);
    }

    public void StopDateChangeMonitor()
    {
        var cancellation = _dateMonitorCancellation;
        _dateMonitorCancellation = null;
        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ShowPreviousMonthAsync()
    {
        _displayedMonth = _displayedMonth.AddMonths(-1);
        UpdateLocalizedText();
        await RefreshAsync();
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ShowNextMonthAsync()
    {
        _displayedMonth = _displayedMonth.AddMonths(1);
        UpdateLocalizedText();
        await RefreshAsync();
    }

    public static DateTime GetWeekStart(DateTime date)
    {
        var mondayOffset = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-mondayOffset);
    }

    private void PublishPresentation(
        IReadOnlyDictionary<DateTime, DailyStatistics> statistics,
        DateTime today)
    {
        Days = BuildCalendarDays(_displayedMonth, today, statistics);
        WeeklyDays = BuildWeeklyDays(today, statistics);
    }

    private static IReadOnlyList<CalendarDayItem> BuildCalendarDays(
        DateTime month,
        DateTime today,
        IReadOnlyDictionary<DateTime, DailyStatistics> statistics)
    {
        var firstDayOffset = ((int)month.DayOfWeek + 6) % 7;
        var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
        var result = new List<CalendarDayItem>(firstDayOffset + daysInMonth + 6);

        for (var index = 0; index < firstDayOffset; index++)
        {
            result.Add(new CalendarDayItem());
        }

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(month.Year, month.Month, day);
            var total = date <= today && statistics.TryGetValue(date, out var value)
                ? value.TotalCaffeineMg
                : 0;
            result.Add(new CalendarDayItem
            {
                Date = date,
                DayNumber = day,
                IsCurrentMonth = true,
                IsToday = date == today,
                TotalCaffeineMg = total,
                Level = CaffeineLevelResolver.ResolveDailyTotal(total)
            });
        }

        while (result.Count % 7 != 0)
        {
            result.Add(new CalendarDayItem());
        }

        return result;
    }

    private IReadOnlyList<WeeklyConsumptionItem> BuildWeeklyDays(
        DateTime today,
        IReadOnlyDictionary<DateTime, DailyStatistics> statistics)
    {
        var weekStart = GetWeekStart(today);
        var result = new List<WeeklyConsumptionItem>(7);
        for (var index = 0; index < 7; index++)
        {
            var date = weekStart.AddDays(index);
            statistics.TryGetValue(date, out var daily);
            result.Add(new WeeklyConsumptionItem
            {
                Date = date,
                DayLabel = GetWeekdayLabel(date.DayOfWeek),
                IsToday = date == today,
                Distribution = daily?.Distribution ?? default
            });
        }

        return result;
    }

    private static IReadOnlyDictionary<DateTime, DailyStatistics> BuildDailyStatistics(
        IEnumerable<CaffeineConsumption> consumptions)
    {
        var result = new Dictionary<DateTime, DailyStatistics>();
        foreach (var consumption in consumptions)
        {
            var localDate = consumption.ConsumedAt.ToLocalTime().Date;
            if (!result.TryGetValue(localDate, out var statistics))
            {
                statistics = new DailyStatistics();
                result.Add(localDate, statistics);
            }

            statistics.Add(consumption);
        }

        return result;
    }

    private void UpdateLocalizedText()
    {
        var title = _displayedMonth.ToString("MMMM yyyy", _localization.CurrentCulture);
        MonthTitle = _localization.CurrentCulture.TextInfo.ToTitleCase(title);
        OnPropertyChanged(nameof(LowRangeLabel));
        OnPropertyChanged(nameof(MediumRangeLabel));
        OnPropertyChanged(nameof(HighRangeLabel));
        OnPropertyChanged(nameof(ExceededRangeLabel));
    }

    private string GetWeekdayLabel(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => _localization["MondayShort"],
        DayOfWeek.Tuesday => _localization["TuesdayShort"],
        DayOfWeek.Wednesday => _localization["WednesdayShort"],
        DayOfWeek.Thursday => _localization["ThursdayShort"],
        DayOfWeek.Friday => _localization["FridayShort"],
        DayOfWeek.Saturday => _localization["SaturdayShort"],
        DayOfWeek.Sunday => _localization["SundayShort"],
        _ => string.Empty
    };

    private void OnCultureChanged(object? sender, EventArgs eventArgs)
    {
        UpdateLocalizedText();
        var today = DateTime.Today;
        WeeklyDays = WeeklyDays.Select(item => new WeeklyConsumptionItem
        {
            Date = item.Date,
            DayLabel = GetWeekdayLabel(item.DayOfWeek),
            IsToday = item.Date == today,
            Distribution = item.Distribution
        }).ToArray();

        if (HasLoadError)
        {
            LoadError = _localization["LoadDataFailed"];
        }
    }

    private static DateTimeOffset ToUtcBoundary(DateTime localDate)
    {
        var unspecified = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
        var localBoundary = new DateTimeOffset(
            unspecified,
            TimeZoneInfo.Local.GetUtcOffset(unspecified));
        return localBoundary.ToUniversalTime();
    }

    private async Task SetLoadingStateAsync(bool isLoading, string? error)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            IsLoading = isLoading;
            LoadError = error;
        });
    }

    private async Task MonitorDateChangeAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(DateChangeCheckInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var today = DateTime.Today;
                if (today == _lastObservedDate)
                {
                    continue;
                }

                _lastObservedDate = today;
                if (_displayedMonth.Year != today.Year || _displayedMonth.Month != today.Month)
                {
                    _displayedMonth = new DateTime(today.Year, today.Month, 1);
                    await MainThread.InvokeOnMainThreadAsync(UpdateLocalizedText);
                }

                await RefreshAsync();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when CalendarPage leaves the visual tree.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Calendar date-change monitor stopped unexpectedly.");
        }
    }

    private sealed class DailyStatistics
    {
        private int _coffeeCount;
        private int _teaCount;
        private int _energyDrinkCount;

        public int TotalCaffeineMg { get; private set; }

        public ConsumptionTypeDistribution Distribution =>
            new(_coffeeCount, _teaCount, _energyDrinkCount);

        public void Add(CaffeineConsumption consumption)
        {
            TotalCaffeineMg = (int)Math.Min(
                int.MaxValue,
                (long)TotalCaffeineMg + Math.Max(0, consumption.CaffeineMg));

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
    }
}
