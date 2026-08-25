using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CoffeeNap.Models;
using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.ViewModels;

public partial class MainPageViewModel : ObservableObject, IDisposable
{
    private const double LowProgressThreshold = 0.40;
    private const double MediumProgressThreshold = 0.70;
    private const double LimitProgressThreshold = 1.00;
    private static readonly TimeSpan RelativeTimeRefreshInterval = TimeSpan.FromSeconds(30);

    private readonly IAppDataService _dataService;
    private readonly ILogger<MainPageViewModel> _logger;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private CancellationTokenSource? _relativeTimeCancellation;
    private DateTime _currentLocalDate = DateTime.Today;
    private double _currentCaffeine;

    private double _dailyCaffeineLimit;

    private CaffeineSourceStatViewModel _coffeeSource =
        new(CaffeineConsumptionType.Coffee, 0, 0);

    private CaffeineSourceStatViewModel _teaSource =
        new(CaffeineConsumptionType.Tea, 0, 0);

    private CaffeineSourceStatViewModel _energyDrinkSource =
        new(CaffeineConsumptionType.EnergyDrink, 0, 0);
    private bool _isReplacingConsumptions;

    private bool _isInitialized;

    private bool _isBusy;

    private string? _initializationError;

    public MainPageViewModel(
        IAppDataService dataService,
        ILogger<MainPageViewModel> logger,
        MainHeaderViewModel header,
        BottomNavigationViewModel navigation)
    {
        _dataService = dataService;
        _logger = logger;
        Header = header;
        Navigation = navigation;
        Navigation.ActiveTab = NavigationTab.Home;
        Consumptions.CollectionChanged += OnConsumptionsCollectionChanged;
    }

    public MainHeaderViewModel Header { get; }

    public BottomNavigationViewModel Navigation { get; }

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
        private set => SetProperty(ref _initializationError, value);
    }

    public double DailyProgress => DailyCaffeineLimit <= 0
        ? 0
        : Math.Clamp(CurrentCaffeine / DailyCaffeineLimit, 0, 1);

    public Color DailyProgressColor
    {
        get
        {
            var ratio = DailyCaffeineLimit <= 0 ? 0 : CurrentCaffeine / DailyCaffeineLimit;
            return ratio switch
            {
                <= LowProgressThreshold => GetResourceColor("CaffeineProgressLow", Colors.Lime),
                <= MediumProgressThreshold => GetResourceColor("CaffeineProgressMedium", Colors.Yellow),
                < LimitProgressThreshold => GetResourceColor("CaffeineProgressHigh", Colors.Orange),
                _ => GetResourceColor("CaffeineProgressLimit", Colors.Red)
            };
        }
    }

    public ObservableCollection<ConsumptionItemViewModel> Consumptions { get; } = [];

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        await _operationLock.WaitAsync();
        try
        {
            if (IsInitialized)
            {
                return;
            }

            IsBusy = true;
            InitializationError = null;
            await _dataService.InitializeAsync();

            var settings = await _dataService.GetSettingsAsync();
            var consumptions = await _dataService.GetConsumptionsAsync();

            DailyCaffeineLimit = settings.DailyCaffeineLimit;
            ReplaceConsumptions(consumptions);
            IsInitialized = true;
        }
        catch (Exception exception)
        {
            InitializationError = "Не удалось загрузить данные";
            _logger.LogError(exception, "Main page initialization failed.");
        }
        finally
        {
            IsBusy = false;
            _operationLock.Release();
        }
    }

    public async Task AddConsumptionAsync(CaffeineConsumption consumption)
    {
        await EnsureInitializedAsync();

        await _operationLock.WaitAsync();
        try
        {
            await _dataService.AddConsumptionAsync(consumption);
            InsertInChronologicalOrder(new ConsumptionItemViewModel(consumption));
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task UpdateConsumptionAsync(CaffeineConsumption consumption)
    {
        await EnsureInitializedAsync();

        await _operationLock.WaitAsync();
        try
        {
            await _dataService.UpdateConsumptionAsync(consumption);
            ReplaceConsumption(new ConsumptionItemViewModel(consumption));
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task DeleteConsumptionAsync(int id)
    {
        await EnsureInitializedAsync();

        await _operationLock.WaitAsync();
        try
        {
            await _dataService.DeleteConsumptionAsync(id);
            var consumption = Consumptions.FirstOrDefault(item => item.Id == id);
            if (consumption is not null)
            {
                Consumptions.Remove(consumption);
            }
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task SaveDailyCaffeineLimitAsync(double limit)
    {
        var settings = await _dataService.GetSettingsAsync();
        settings.DailyCaffeineLimit = limit;
        await _dataService.SaveSettingsAsync(settings);
        DailyCaffeineLimit = limit;
    }

    public void StartRelativeTimeTimer()
    {
        if (_relativeTimeCancellation is { IsCancellationRequested: false })
        {
            return;
        }

        _relativeTimeCancellation = new CancellationTokenSource();
        RefreshTimeDependentState();
        _ = RunRelativeTimeTimerAsync(_relativeTimeCancellation.Token);
    }

    public void StopRelativeTimeTimer()
    {
        var cancellation = _relativeTimeCancellation;
        _relativeTimeCancellation = null;
        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    public void Dispose()
    {
        StopRelativeTimeTimer();
        Consumptions.CollectionChanged -= OnConsumptionsCollectionChanged;
        _operationLock.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task EnsureInitializedAsync()
    {
        await InitializeAsync();
        if (!IsInitialized)
        {
            throw new InvalidOperationException("Application data is not initialized.");
        }
    }

    private void ReplaceConsumptions(IEnumerable<CaffeineConsumption> consumptions)
    {
        _isReplacingConsumptions = true;
        try
        {
            Consumptions.Clear();
            foreach (var consumption in consumptions.OrderByDescending(item => item.ConsumedAt))
            {
                Consumptions.Add(new ConsumptionItemViewModel(consumption));
            }
        }
        finally
        {
            _isReplacingConsumptions = false;
        }

        RefreshConsumptionDerivedState();
    }

    private void InsertInChronologicalOrder(ConsumptionItemViewModel consumption)
    {
        var index = 0;
        while (index < Consumptions.Count &&
               Consumptions[index].ConsumedAt > consumption.ConsumedAt)
        {
            index++;
        }

        Consumptions.Insert(index, consumption);
    }

    private void ReplaceConsumption(ConsumptionItemViewModel consumption)
    {
        _isReplacingConsumptions = true;
        try
        {
            var existing = Consumptions.FirstOrDefault(item => item.Id == consumption.Id);
            if (existing is not null)
            {
                Consumptions.Remove(existing);
            }

            InsertInChronologicalOrder(consumption);
        }
        finally
        {
            _isReplacingConsumptions = false;
        }

        RefreshConsumptionDerivedState();
    }

    private void OnConsumptionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_isReplacingConsumptions)
        {
            RefreshConsumptionDerivedState();
        }
    }

    private void RefreshConsumptionDerivedState()
    {
        var models = Consumptions.Select(item => item.Model).ToArray();
        RecalculateDailyCaffeine(models, DateTimeOffset.Now);
        RecalculateSourceStatistics(models);
    }

    private void RecalculateDailyCaffeine(
        IEnumerable<CaffeineConsumption> consumptions,
        DateTimeOffset now)
    {
        CurrentCaffeine = CaffeineStatisticsCalculator.CalculateDailyCaffeine(consumptions, now);
    }

    private void RecalculateSourceStatistics(IReadOnlyCollection<CaffeineConsumption> consumptions)
    {
        var counts = CaffeineStatisticsCalculator.CountBySource(consumptions);
        var totalCount = consumptions.Count;
        CoffeeSource = new CaffeineSourceStatViewModel(
            CaffeineConsumptionType.Coffee,
            counts[CaffeineConsumptionType.Coffee],
            totalCount);
        TeaSource = new CaffeineSourceStatViewModel(
            CaffeineConsumptionType.Tea,
            counts[CaffeineConsumptionType.Tea],
            totalCount);
        EnergyDrinkSource = new CaffeineSourceStatViewModel(
            CaffeineConsumptionType.EnergyDrink,
            counts[CaffeineConsumptionType.EnergyDrink],
            totalCount);
    }

    private async Task RunRelativeTimeTimerAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(RelativeTimeRefreshInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await MainThread.InvokeOnMainThreadAsync(RefreshTimeDependentState);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when the page leaves the visual tree.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Relative-time timer stopped unexpectedly.");
        }
    }

    private void RefreshTimeDependentState()
    {
        foreach (var consumption in Consumptions)
        {
            consumption.RefreshRelativeTime();
        }

        var now = DateTimeOffset.Now;
        var today = now.ToLocalTime().Date;
        if (today != _currentLocalDate)
        {
            _currentLocalDate = today;
            RecalculateDailyCaffeine(
                Consumptions.Select(item => item.Model),
                now);
        }
    }

    private static Color GetResourceColor(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : fallback;
}
