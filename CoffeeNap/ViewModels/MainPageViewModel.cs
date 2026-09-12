using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CoffeeNap.Models;
using CoffeeNap.Services;
using CoffeeNap.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private static readonly TimeSpan RelativeTimeRefreshInterval = TimeSpan.FromSeconds(30);

    private readonly IAppDataService _dataService;
    private readonly LocalizationService _localization;
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
    private ObservableCollection<ConsumptionItemViewModel> _consumptions = [];

    private bool _isInitialized;

    private bool _isBusy;

    private string? _initializationError;

    public MainPageViewModel(
        IAppDataService dataService,
        LocalizationService localization,
        ILogger<MainPageViewModel> logger,
        MainHeaderViewModel header)
    {
        _dataService = dataService;
        _localization = localization;
        _logger = logger;
        Header = header;
        Consumptions.CollectionChanged += OnConsumptionsCollectionChanged;
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

    // Инициализация. Подгрузка данных из бд
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
            InitializationError = _localization["LoadDataFailed"];
            _logger.LogError(exception, "Main page initialization failed.");
        }
        finally
        {
            IsBusy = false;
            _operationLock.Release();
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task DeleteConsumptionAsync(ConsumptionItemViewModel? consumption)
    {
        if (consumption is null || consumption.Id <= 0)
        {
            return;
        }

        await _operationLock.WaitAsync();
        try
        {
            if (Consumptions.All(item => item.Id != consumption.Id))
            {
                return;
            }

            await _dataService.DeleteConsumptionAsync(consumption.Id);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Consumption {ConsumptionId} delete failed.", consumption.Id);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    // Запуск счёта времени употребления
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

    private void ReplaceConsumptions(IEnumerable<CaffeineConsumption> consumptions)
    {
        // Publish one populated source instead of N native CollectionView insertions.
        var replacement = new ObservableCollection<ConsumptionItemViewModel>(
            consumptions.OrderByDescending(item => item.ConsumedAt)
                .Select(item => new ConsumptionItemViewModel(item)));
        _consumptions.CollectionChanged -= OnConsumptionsCollectionChanged;
        _consumptions = replacement;
        _consumptions.CollectionChanged += OnConsumptionsCollectionChanged;
        OnPropertyChanged(nameof(Consumptions));
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

    private void OnConsumptionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshConsumptionDerivedState();
    }

    private void OnConsumptionAdded(object? sender, CaffeineConsumption consumption)
    {
        if (!IsInitialized || Consumptions.Any(item => item.Id == consumption.Id))
        {
            return;
        }

        void AddToRuntimeState() =>
            InsertInChronologicalOrder(new ConsumptionItemViewModel(consumption));

        if (MainThread.IsMainThread)
        {
            AddToRuntimeState();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(AddToRuntimeState);
        }
    }

    private void OnConsumptionDeleted(object? sender, CaffeineConsumption consumption)
    {
        void RemoveFromRuntimeState()
        {
            var item = Consumptions.FirstOrDefault(candidate => candidate.Id == consumption.Id);
            if (item is not null)
            {
                Consumptions.Remove(item);
            }
        }

        if (MainThread.IsMainThread)
        {
            RemoveFromRuntimeState();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(RemoveFromRuntimeState);
        }
    }

    private void OnCultureChanged(object? sender, EventArgs eventArgs)
    {
        foreach (var consumption in Consumptions)
        {
            consumption.RefreshLocalizedState();
        }

        RecalculateSourceStatistics(Consumptions.Select(item => item.Model).ToArray());
        if (InitializationError is not null)
        {
            InitializationError = _localization["LoadDataFailed"];
        }
    }

    private void OnUserDataDeleted(object? sender, EventArgs eventArgs)
    {
        StopRelativeTimeTimer();
        Consumptions.Clear();
        CurrentCaffeine = 0;
        DailyCaffeineLimit = AppSettings.DefaultDailyCaffeineLimit;
        InitializationError = null;
        IsBusy = false;
        IsInitialized = false;
        RecalculateSourceStatistics([]);
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
        var distribution = ConsumptionTypeDistributionCalculator.Calculate(consumptions);
        CoffeeSource = new CaffeineSourceStatViewModel(
            CaffeineConsumptionType.Coffee,
            distribution.CoffeeCount,
            distribution.TotalCount);
        TeaSource = new CaffeineSourceStatViewModel(
            CaffeineConsumptionType.Tea,
            distribution.TeaCount,
            distribution.TotalCount);
        EnergyDrinkSource = new CaffeineSourceStatViewModel(
            CaffeineConsumptionType.EnergyDrink,
            distribution.EnergyDrinkCount,
            distribution.TotalCount);
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

}
