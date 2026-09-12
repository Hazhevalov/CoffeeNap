using CoffeeNap.Models;
using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.ViewModels;

public partial class CalendarPageViewModel : ObservableObject
{
    private static readonly TimeSpan DateChangeCheckInterval = TimeSpan.FromMinutes(1);

    private readonly CalendarStatisticsService _statisticsService;
    private readonly LocalizationService _localization;
    private readonly ILogger<CalendarPageViewModel> _logger;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private IReadOnlyList<CalendarDayItem> _days = [];
    private IReadOnlyList<WeeklyConsumptionItem> _weeklyDays = [];
    private DateTime _requestedMonth;
    private DateTime _publishedMonth;
    private DateTime _presentationDate;
    private DateTime _weekPresentationDate;
    private CalendarMonthStatistics? _publishedMonthStatistics;
    private CalendarWeekStatistics? _publishedWeekStatistics;
    private string _monthTitle = string.Empty;
    private string? _loadError;
    private bool _isLoading;
    private int _monthRequestVersion;
    private CancellationTokenSource? _dateMonitorCancellation;
    private DateTime _lastObservedDate = DateTime.Today;

    public CalendarPageViewModel(
        CalendarStatisticsService statisticsService,
        LocalizationService localization,
        ILogger<CalendarPageViewModel> logger,
        MainHeaderViewModel header)
    {
        _statisticsService = statisticsService;
        _localization = localization;
        _logger = logger;
        Header = header;

        var today = DateTime.Today;
        _requestedMonth = new DateTime(today.Year, today.Month, 1);
        _publishedMonth = _requestedMonth;
        _presentationDate = today;
        MonthTitle = FormatMonthTitle(_publishedMonth);
        Days = BuildCalendarDays(_publishedMonth, today, EmptyStatistics);
        WeeklyDays = BuildWeeklyDays(today, EmptyStatistics);
        _localization.CultureChanged += OnCultureChanged;
    }

    private static IReadOnlyDictionary<DateOnly, CalendarDailyStatistics> EmptyStatistics { get; } =
        new Dictionary<DateOnly, CalendarDailyStatistics>();

    public MainHeaderViewModel Header { get; }

    public IReadOnlyList<CalendarDayItem> Days
    {
        get => _days;
        private set => SetProperty(ref _days, value);
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

    /// <summary>
    /// Reuses cached month/week instances on repeated appearances and reloads
    /// only the range invalidated by a data change.
    /// </summary>
    public Task RefreshAsync() => LoadCurrentStateAsync(prefetchAdjacentMonths: true);

    public Task WarmUpAsync() => LoadCurrentStateAsync(prefetchAdjacentMonths: false);

    private async Task LoadCurrentStateAsync(bool prefetchAdjacentMonths)
    {
        await _initializationLock.WaitAsync();
        try
        {
            await SetLoadingStateAsync(true, null);
            var today = DateTime.Today;
            var targetMonth = _requestedMonth;
            var result = await _statisticsService.GetInitialAsync(targetMonth, today)
                .ConfigureAwait(false);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (targetMonth == _requestedMonth)
                {
                    PublishMonthIfChanged(result.Month, today);
                }

                PublishWeekIfChanged(result.Week, today);
                LoadError = null;
            });

            if (prefetchAdjacentMonths)
            {
                _statisticsService.PrefetchAdjacentMonths(targetMonth);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Calendar refresh failed.");
            await SetLoadingStateAsync(false, _localization["LoadDataFailed"]);
        }
        finally
        {
            await SetLoadingStateAsync(false, LoadError);
            _initializationLock.Release();
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

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task ShowPreviousMonthAsync() => ChangeMonthAsync(-1);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task ShowNextMonthAsync() => ChangeMonthAsync(1);

    public static DateTime GetWeekStart(DateTime date)
    {
        var mondayOffset = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-mondayOffset);
    }

    private async Task ChangeMonthAsync(int offset)
    {
        var targetMonth = _requestedMonth.AddMonths(offset);
        _requestedMonth = targetMonth;
        var requestVersion = Interlocked.Increment(ref _monthRequestVersion);
        IsLoading = true;
        LoadError = null;

        try
        {
            var statistics = await _statisticsService.GetMonthAsync(targetMonth)
                .ConfigureAwait(false);
            if (requestVersion != Volatile.Read(ref _monthRequestVersion) ||
                targetMonth != _requestedMonth)
            {
                return;
            }

            var today = DateTime.Today;
            await MainThread.InvokeOnMainThreadAsync(() =>
                PublishMonthIfChanged(statistics, today));
            _statisticsService.PrefetchAdjacentMonths(targetMonth);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Calendar month load failed for {Month:yyyy-MM}.", targetMonth);
            if (requestVersion == Volatile.Read(ref _monthRequestVersion))
            {
                await SetLoadingStateAsync(false, _localization["LoadDataFailed"]);
            }
        }
        finally
        {
            if (requestVersion == Volatile.Read(ref _monthRequestVersion))
            {
                await SetLoadingStateAsync(false, LoadError);
            }
        }
    }

    private void PublishMonthIfChanged(
        CalendarMonthStatistics statistics,
        DateTime today)
    {
        if (ReferenceEquals(_publishedMonthStatistics, statistics) &&
            _presentationDate == today)
        {
            return;
        }

        var models = BuildCalendarDays(statistics.Month, today, statistics.Days);
        _publishedMonthStatistics = statistics;
        _publishedMonth = statistics.Month;
        _presentationDate = today;
        MonthTitle = FormatMonthTitle(_publishedMonth);
        Days = models;
    }

    private void PublishWeekIfChanged(
        CalendarWeekStatistics statistics,
        DateTime today)
    {
        if (ReferenceEquals(_publishedWeekStatistics, statistics) &&
            _weekPresentationDate == today)
        {
            return;
        }

        _publishedWeekStatistics = statistics;
        _weekPresentationDate = today;
        WeeklyDays = BuildWeeklyDays(today, statistics.Days);
    }

    private static IReadOnlyList<CalendarDayItem> BuildCalendarDays(
        DateTime month,
        DateTime today,
        IReadOnlyDictionary<DateOnly, CalendarDailyStatistics> statistics)
    {
        const int slotCount = 42;
        var firstDayOffset = ((int)month.DayOfWeek + 6) % 7;
        var firstSlotDate = month.AddDays(-firstDayOffset);
        var result = new CalendarDayItem[slotCount];
        for (var index = 0; index < slotCount; index++)
        {
            var date = firstSlotDate.AddDays(index);
            var isCurrentMonth = date.Year == month.Year && date.Month == month.Month;
            var dateOnly = DateOnly.FromDateTime(date);
            var total = isCurrentMonth && date <= today && statistics.TryGetValue(dateOnly, out var value)
                ? value.TotalCaffeineMg
                : 0;
            result[index] = new CalendarDayItem
            {
                Date = date,
                DayNumber = isCurrentMonth ? date.Day : 0,
                IsCurrentMonth = isCurrentMonth,
                IsToday = isCurrentMonth && date == today,
                TotalCaffeineMg = total,
                Level = CaffeineLevelResolver.ResolveDailyTotal(total)
            };
        }

        return result;
    }

    private IReadOnlyList<WeeklyConsumptionItem> BuildWeeklyDays(
        DateTime today,
        IReadOnlyDictionary<DateOnly, CalendarDailyStatistics> statistics)
    {
        var weekStart = GetWeekStart(today);
        var result = new WeeklyConsumptionItem[7];
        for (var index = 0; index < result.Length; index++)
        {
            var date = weekStart.AddDays(index);
            statistics.TryGetValue(DateOnly.FromDateTime(date), out var daily);
            result[index] = new WeeklyConsumptionItem
            {
                Date = date,
                DayLabel = GetWeekdayLabel(date.DayOfWeek),
                IsToday = date == today,
                Distribution = daily.Distribution
            };
        }

        return result;
    }

    private string FormatMonthTitle(DateTime month)
    {
        var title = month.ToString("MMMM yyyy", _localization.CurrentCulture);
        return _localization.CurrentCulture.TextInfo.ToTitleCase(title);
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
        MonthTitle = FormatMonthTitle(_publishedMonth);
        OnPropertyChanged(nameof(LowRangeLabel));
        OnPropertyChanged(nameof(MediumRangeLabel));
        OnPropertyChanged(nameof(HighRangeLabel));
        OnPropertyChanged(nameof(ExceededRangeLabel));

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
                if (_requestedMonth.Year != today.Year || _requestedMonth.Month != today.Month)
                {
                    _requestedMonth = new DateTime(today.Year, today.Month, 1);
                    Interlocked.Increment(ref _monthRequestVersion);
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
}
