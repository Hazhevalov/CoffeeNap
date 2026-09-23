using System.Collections.ObjectModel;
using CoffeeNap.Models;
using CoffeeNap.Services;
using CoffeeNap.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private const int PageSize = 40;
    private readonly IAppDataService _dataService;
    private readonly LocalizationService _localization;
    private readonly IDialogService _dialogs;
    private readonly ILogger<MainPageViewModel> _logger;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private CancellationTokenSource? _relativeTimeCancellation;
    private readonly Dictionary<int, ConsumptionItemViewModel> _itemsById = [];
    private PagedObservableCollection<ConsumptionItemViewModel> _consumptions = [];
    private ConsumptionTypeDistribution _distribution;
    private ConsumptionCursor? _cursor;
    private bool _hasMore;
    private int _firstVisible;
    private int _lastVisible = -1;
    private int _dataVersion;
    private int _summaryVersion;
    private DateTime _summaryDate;
    private string _zoneKey = string.Empty;
    private double _currentCaffeine;
    private double _dailyCaffeineLimit;
    private bool _isInitialized;
    private bool _isBusy;
    private string? _initializationError;
    private CaffeineSourceStatViewModel _coffeeSource = new(CaffeineConsumptionType.Coffee, 0, 0);
    private CaffeineSourceStatViewModel _teaSource = new(CaffeineConsumptionType.Tea, 0, 0);
    private CaffeineSourceStatViewModel _energyDrinkSource = new(CaffeineConsumptionType.EnergyDrink, 0, 0);

    public MainPageViewModel(IAppDataService dataService, LocalizationService localization,
        ILogger<MainPageViewModel> logger, MainHeaderViewModel header, IDialogService dialogs)
    {
        _dataService = dataService;
        _localization = localization;
        _logger = logger;
        _dialogs = dialogs;
        Header = header;
        _dataService.ConsumptionAdded += OnConsumptionAdded;
        _dataService.ConsumptionDeleted += OnConsumptionDeleted;
        _dataService.UserDataDeleted += OnUserDataDeleted;
        _localization.CultureChanged += OnCultureChanged;
    }

    public MainHeaderViewModel Header { get; }

    public double CurrentCaffeine
    {
        get => _currentCaffeine;
        private set
        {
            if (SetProperty(ref _currentCaffeine, value))
            {
                OnPropertyChanged(nameof(DailyProgress));
                OnPropertyChanged(nameof(DailyProgressColor));
            }
        }
    }

    public double DailyCaffeineLimit
    {
        get => _dailyCaffeineLimit;
        private set
        {
            if (SetProperty(ref _dailyCaffeineLimit, value))
            {
                OnPropertyChanged(nameof(DailyLimitDisplay));
                OnPropertyChanged(nameof(DailyProgress));
                OnPropertyChanged(nameof(DailyProgressColor));
            }
        }
    }

    public CaffeineSourceStatViewModel CoffeeSource
    {
        get => _coffeeSource;
        private set => SetProperty(ref _coffeeSource, value);
    }

    public CaffeineSourceStatViewModel TeaSource
    {
        get => _teaSource;
        private set => SetProperty(ref _teaSource, value);
    }

    public CaffeineSourceStatViewModel EnergyDrinkSource
    {
        get => _energyDrinkSource;
        private set => SetProperty(ref _energyDrinkSource, value);
    }

    public bool IsInitialized
    {
        get => _isInitialized;
        private set => SetProperty(ref _isInitialized, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string? InitializationError
    {
        get => _initializationError;
        private set
        {
            if (SetProperty(ref _initializationError, value)) OnPropertyChanged(nameof(HasLoadError));
        }
    }

    public double DailyProgress => DailyCaffeineLimit <= 0
        ? 0
        : Math.Clamp(CurrentCaffeine / DailyCaffeineLimit, 0, 1);

    // Полоска дневной нормы кофеина
    public Color DailyProgressColor
    {
        get
        {
            var ratio = DailyCaffeineLimit <= 0 ? 0 : CurrentCaffeine / DailyCaffeineLimit;
            return CaffeineLevelColorProvider.GetColor(
                CaffeineLevelResolver.ResolveProgress(ratio));
        }
    }

    public ObservableCollection<ConsumptionItemViewModel> Consumptions => _consumptions;


    public bool HasLoadError => !string.IsNullOrEmpty(InitializationError);
    public bool HasMore => _hasMore;
    public string DailyLimitDisplay => $"{DailyCaffeineLimit:0} {_localization["MilligramShort"]}";

    public async Task InitializeAsync()
    {
        await _operationLock.WaitAsync();
        try
        {
            IsBusy = true;
            InitializationError = null;
            if (!IsInitialized)
            {
                // Events during initial loading invalidate the whole snapshot.
                int version;
                IReadOnlyList<CaffeineConsumption> rows;
                do
                {
                    version = Volatile.Read(ref _dataVersion);
                    var settings = await _dataService.GetSettingsAsync();
                    DailyCaffeineLimit = settings.DailyCaffeineLimit;
                    rows = await _dataService.GetConsumptionsPageAsync(null, PageSize + 1);
                    await RefreshOverviewCoreAsync();
                } while (version != Volatile.Read(ref _dataVersion));
                _itemsById.Clear();
                _consumptions = new PagedObservableCollection<ConsumptionItemViewModel>(
                    rows.Take(PageSize).Select(item => new ConsumptionItemViewModel(item)));
                foreach (var item in _consumptions) _itemsById[item.Id] = item;
                UpdateCursor(rows);
                OnPropertyChanged(nameof(Consumptions));
                IsInitialized = true;
            }
            else
            {
                await RefreshOverviewCoreAsync();
            }
        }
        catch (Exception exception) { ShowLoadError(exception); }
        finally { IsBusy = false; _operationLock.Release(); }
    }

    [RelayCommand]
    private Task RetryAsync() => !IsInitialized ? InitializeAsync() : RetryLoadedStateAsync();

    private async Task RetryLoadedStateAsync()
    {
        await InitializeAsync();
        if (!HasLoadError && _hasMore) await LoadMoreAsync();
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!IsInitialized || !_hasMore || IsBusy) return;
        await _operationLock.WaitAsync();
        try
        {
            if (!_hasMore) return;
            IsBusy = true;
            InitializationError = null;
            IReadOnlyList<CaffeineConsumption> rows;
            int version;
            do
            {
                version = Volatile.Read(ref _dataVersion);
                rows = await _dataService.GetConsumptionsPageAsync(_cursor, PageSize + 1);
            } while (version != Volatile.Read(ref _dataVersion));
            var additions = new List<ConsumptionItemViewModel>(PageSize);
            foreach (var row in rows.Take(PageSize))
            {
                if (_itemsById.ContainsKey(row.Id)) continue;
                var item = new ConsumptionItemViewModel(row);
                _itemsById.Add(item.Id, item);
                additions.Add(item);
            }
            _consumptions.AddPage(additions);
            UpdateCursor(rows);
        }
        catch (Exception exception) { ShowLoadError(exception); }
        finally { IsBusy = false; _operationLock.Release(); }
    }

    private void UpdateCursor(IReadOnlyList<CaffeineConsumption> rows)
    {
        _hasMore = rows.Count > PageSize;
        var last = rows.Take(PageSize).LastOrDefault();
        if (last is not null) _cursor = new ConsumptionCursor(last.ConsumedAt, last.Id);
        OnPropertyChanged(nameof(HasMore));
    }

    [RelayCommand]
    private async Task DeleteConsumptionAsync(ConsumptionItemViewModel? item)
    {
        if (item is null || !_itemsById.ContainsKey(item.Id)) return;
        try { await _dataService.DeleteConsumptionAsync(item.Id); }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Consumption deletion failed.");
            await _dialogs.ShowErrorAsync(_localization["ErrorTitle"],
                _localization["DeleteFailed"], _localization["Ok"]);
        }
    }

    private void OnConsumptionAdded(object? sender, CaffeineConsumption item) => QueueChange(item, added: true);
    private void OnConsumptionDeleted(object? sender, CaffeineConsumption item) => QueueChange(item, added: false);

    public void Release()
    {
        StopRelativeTimeTimer();
        _dataService.ConsumptionAdded -= OnConsumptionAdded;
        _dataService.ConsumptionDeleted -= OnConsumptionDeleted;
        _dataService.UserDataDeleted -= OnUserDataDeleted;
        _localization.CultureChanged -= OnCultureChanged;
    }

    private void QueueChange(CaffeineConsumption item, bool added)
    {
        var version = Interlocked.Increment(ref _dataVersion);
        if (!IsInitialized) return;
        _ = MainThread.InvokeOnMainThreadAsync(() => ApplyChangeAsync(item, added, version));
    }

    private async Task ApplyChangeAsync(CaffeineConsumption row, bool added, int version)
    {
        await _operationLock.WaitAsync();
        try
        {
            if (!IsInitialized) return;
            if (added && !_itemsById.ContainsKey(row.Id))
            {
                // An older imported record outside the loaded range will arrive via paging.
                if (!_hasMore || _cursor is not { } cursor || row.ConsumedAt > cursor.ConsumedAt ||
                    (row.ConsumedAt == cursor.ConsumedAt && row.Id >= cursor.Id))
                {
                    var item = new ConsumptionItemViewModel(row);
                    var index = 0;
                    while (index < Consumptions.Count && (Consumptions[index].ConsumedAt > row.ConsumedAt ||
                        (Consumptions[index].ConsumedAt == row.ConsumedAt && Consumptions[index].Id > row.Id))) index++;
                    _itemsById.Add(row.Id, item);
                    Consumptions.Insert(index, item);
                }
            }
            else if (!added && _itemsById.Remove(row.Id, out var existing)) Consumptions.Remove(existing);

            if (version > _summaryVersion)
            {
                var change = added ? 1 : -1;
                _distribution = row.Type switch
                {
                    CaffeineConsumptionType.Coffee => _distribution with { CoffeeCount = Math.Max(0, _distribution.CoffeeCount + change) },
                    CaffeineConsumptionType.Tea => _distribution with { TeaCount = Math.Max(0, _distribution.TeaCount + change) },
                    CaffeineConsumptionType.EnergyDrink => _distribution with { EnergyDrinkCount = Math.Max(0, _distribution.EnergyDrinkCount + change) },
                    _ => _distribution
                };
                if (row.ConsumedAt.ToLocalTime().Date == _summaryDate && row.ConsumedAt <= DateTimeOffset.Now)
                    CurrentCaffeine = Math.Max(0, CurrentCaffeine + change * (double)Math.Max(0, row.CaffeineMg));
                PublishDistribution();
            }
        }
        catch (Exception exception) { ShowLoadError(exception); }
        finally { _operationLock.Release(); }
    }

    private async Task RefreshOverviewCoreAsync()
    {
        int version;
        ConsumptionOverview overview;
        do
        {
            version = Volatile.Read(ref _dataVersion);
            _zoneKey = LocalCalendarTime.RefreshZoneKey();
            var now = DateTimeOffset.Now;
            _summaryDate = now.ToLocalTime().Date;
            overview = await _dataService.GetConsumptionOverviewAsync(now);
        } while (version != Volatile.Read(ref _dataVersion));
        _summaryVersion = version;
        CurrentCaffeine = overview.DailyCaffeineMg;
        _distribution = overview.Distribution;
        PublishDistribution();
    }

    private void PublishDistribution()
    {
        CoffeeSource = new(CaffeineConsumptionType.Coffee, _distribution.CoffeeCount, _distribution.TotalCount);
        TeaSource = new(CaffeineConsumptionType.Tea, _distribution.TeaCount, _distribution.TotalCount);
        EnergyDrinkSource = new(CaffeineConsumptionType.EnergyDrink, _distribution.EnergyDrinkCount, _distribution.TotalCount);
    }

    public void SetVisibleRange(int first, int last)
    {
        _firstVisible = Math.Max(0, first);
        _lastVisible = last;
        RefreshVisibleTimes();
    }

    private void RefreshVisibleTimes()
    {
        for (var index = _firstVisible; index <= _lastVisible && index < Consumptions.Count; index++)
            Consumptions[index].RefreshRelativeTime();
    }

    public void StartRelativeTimeTimer()
    {
        if (_relativeTimeCancellation is not null) return;
        _relativeTimeCancellation = new CancellationTokenSource();
        RefreshVisibleTimes();
        _ = RunRelativeTimeTimerAsync(_relativeTimeCancellation.Token);
    }

    public void StopRelativeTimeTimer()
    {
        var cancellation = _relativeTimeCancellation;
        _relativeTimeCancellation = null;
        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    private async Task RunRelativeTimeTimerAsync(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(token))
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (token.IsCancellationRequested) return;
                    RefreshVisibleTimes();
                    if (_summaryDate != DateTime.Today || _zoneKey != LocalCalendarTime.RefreshZoneKey())
                        await InitializeAsync();
                });
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { _logger.LogError(exception, "History timer failed."); }
    }

    private void ShowLoadError(Exception exception)
    {
        InitializationError = _localization["LoadDataFailed"];
        _logger.LogError(exception, "History refresh failed.");
    }

    private void OnCultureChanged(object? sender, EventArgs args)
    {
        foreach (var item in Consumptions) item.RefreshLocalizedState();
        PublishDistribution();
        OnPropertyChanged(nameof(DailyLimitDisplay));
        if (HasLoadError) InitializationError = _localization["LoadDataFailed"];
    }

    private void OnUserDataDeleted(object? sender, EventArgs args)
    {
        Interlocked.Increment(ref _dataVersion);
        StopRelativeTimeTimer();
        IsInitialized = false;
        Consumptions.Clear();
        _itemsById.Clear();
        _cursor = null;
        _hasMore = false;
        OnPropertyChanged(nameof(HasMore));
        CurrentCaffeine = 0;
        _distribution = default;
        PublishDistribution();
        InitializationError = null;
    }
}
