using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using CoffeeNap.Models;
using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoffeeNap.ViewModels;

/// <summary>
/// Runtime-состояние главного экрана. Persistent-данные загружаются и изменяются
/// только через IAppDataService, а статистика рассчитывается в памяти.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IAppDataService dataService;
    private readonly SemaphoreSlim operationLock = new(1, 1);
    private readonly HashSet<CaffeineConsumption> subscribedConsumptions = [];
    private CancellationTokenSource? periodicUpdateCancellation;
    private DateTime currentLocalDate = DateTime.Today;
    private double currentCaffeine;
    private double dailyCaffeineLimit;
    private CaffeineSourceStat coffeeSource = null!;
    private CaffeineSourceStat teaSource = null!;
    private CaffeineSourceStat energyDrinkSource = null!;
    private bool isLoadingConsumptions;
    private bool isInitialized;
    private bool isBusy;
    private string? initializationError;
    private string userName = "Пользователь";

    public MainViewModel(IAppDataService dataService)
    {
        this.dataService = dataService;
        SourceStats = [];
        Consumptions = [];
        Consumptions.CollectionChanged += OnConsumptionsCollectionChanged;
        RecalculateSourceStatistics();
    }

    public string UserName
    {
        get => userName;
        private set => SetProperty(ref userName, value);
    }

    public double CurrentCaffeine
    {
        get => currentCaffeine;
        private set
        {
            if (SetProperty(ref currentCaffeine, value))
            {
                OnPropertyChanged(nameof(DailyProgress));
                OnPropertyChanged(nameof(DailyProgressColor));
            }
        }
    }

    public double DailyCaffeineLimit
    {
        get => dailyCaffeineLimit;
        private set
        {
            if (SetProperty(ref dailyCaffeineLimit, value))
            {
                OnPropertyChanged(nameof(DailyProgress));
                OnPropertyChanged(nameof(DailyProgressColor));
            }
        }
    }

    public CaffeineSourceStat CoffeeSource
    {
        get => coffeeSource;
        private set => SetProperty(ref coffeeSource, value);
    }

    public CaffeineSourceStat TeaSource
    {
        get => teaSource;
        private set => SetProperty(ref teaSource, value);
    }

    public CaffeineSourceStat EnergyDrinkSource
    {
        get => energyDrinkSource;
        private set => SetProperty(ref energyDrinkSource, value);
    }

    public bool IsInitialized
    {
        get => isInitialized;
        private set => SetProperty(ref isInitialized, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        private set => SetProperty(ref isBusy, value);
    }

    public string? InitializationError
    {
        get => initializationError;
        private set => SetProperty(ref initializationError, value);
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
                <= 0.40 => GetResourceColor("CaffeineProgressLow", Colors.Lime),
                <= 0.70 => GetResourceColor("CaffeineProgressMedium", Colors.Yellow),
                < 1.00 => GetResourceColor("CaffeineProgressHigh", Colors.Orange),
                _ => GetResourceColor("CaffeineProgressLimit", Colors.Red)
            };
        }
    }

    public ObservableCollection<CaffeineSourceStat> SourceStats { get; }

    public ObservableCollection<CaffeineConsumption> Consumptions { get; }

    /// <summary>Загружает профиль, settings и историю ровно один раз.</summary>
    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        await operationLock.WaitAsync();
        try
        {
            if (IsInitialized)
            {
                return;
            }

            IsBusy = true;
            InitializationError = null;
            await dataService.InitializeAsync();

            var profile = await dataService.GetUserProfileAsync();
            var settings = await dataService.GetSettingsAsync();
            var consumptions = await dataService.GetConsumptionsAsync();

            UserName = string.IsNullOrWhiteSpace(profile?.UserName)
                ? "Пользователь"
                : profile.UserName;
            DailyCaffeineLimit = settings.DailyCaffeineLimit;
            ReplaceConsumptions(consumptions);
            IsInitialized = true;
        }
        catch (Exception exception)
        {
            InitializationError = "Не удалось загрузить данные";
#if DEBUG
            Debug.WriteLine($"MainViewModel initialization failed: {exception}");
#endif
        }
        finally
        {
            IsBusy = false;
            operationLock.Release();
        }
    }

    /// <summary>Сохраняет новую запись до добавления её в UI-коллекцию.</summary>
    public async Task AddConsumptionAsync(CaffeineConsumption consumption)
    {
        await InitializeAsync();
        if (!IsInitialized)
        {
            throw new InvalidOperationException("Application data is not initialized.");
        }

        await operationLock.WaitAsync();
        try
        {
            await dataService.AddConsumptionAsync(consumption);
            InsertInChronologicalOrder(consumption);
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <summary>Сохраняет изменённую запись и затем обновляет её позицию/статистику.</summary>
    public async Task UpdateConsumptionAsync(CaffeineConsumption consumption)
    {
        await InitializeAsync();
        await operationLock.WaitAsync();
        try
        {
            await dataService.UpdateConsumptionAsync(consumption);
            ReorderConsumption(consumption);
            RecalculateSourceStatistics();
            RecalculateDailyCaffeine(DateTimeOffset.Now);
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <summary>Удаляет запись из SQLite до удаления из UI-коллекции.</summary>
    public async Task DeleteConsumptionAsync(int id)
    {
        await InitializeAsync();
        await operationLock.WaitAsync();
        try
        {
            await dataService.DeleteConsumptionAsync(id);
            var consumption = Consumptions.FirstOrDefault(item => item.Id == id);
            if (consumption is not null)
            {
                Consumptions.Remove(consumption);
            }
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <summary>Сохраняет настройку до обновления отображаемого значения.</summary>
    public async Task SaveDailyCaffeineLimitAsync(double limit)
    {
        var settings = await dataService.GetSettingsAsync();
        settings.DailyCaffeineLimit = limit;
        await dataService.SaveSettingsAsync(settings);
        DailyCaffeineLimit = limit;
    }

    /// <summary>Перечитывает небольшую profile-row после будущего Settings UI.</summary>
    public async Task RefreshUserProfileAsync()
    {
        var profile = await dataService.GetUserProfileAsync();
        UserName = string.IsNullOrWhiteSpace(profile?.UserName)
            ? "Пользователь"
            : profile.UserName;
    }

    [RelayCommand]
    private Task OpenSettingsAsync() => Shell.Current.GoToAsync(nameof(SettingsPage));

    [RelayCommand]
    private Task OpenAddConsumptionAsync() => Shell.Current.GoToAsync(nameof(AddConsumptionPage));

    [RelayCommand]
    private Task OpenCalendarAsync() => Shell.Current.GoToAsync(nameof(CalendarPage));

    public void StartPeriodicUpdates()
    {
        if (periodicUpdateCancellation is { IsCancellationRequested: false })
        {
            return;
        }

        periodicUpdateCancellation = new CancellationTokenSource();
        RefreshPeriodicData();
        _ = RunPeriodicUpdatesAsync(periodicUpdateCancellation.Token);
    }

    public void StopPeriodicUpdates()
    {
        var cancellation = periodicUpdateCancellation;
        periodicUpdateCancellation = null;
        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    private void ReplaceConsumptions(IEnumerable<CaffeineConsumption> consumptions)
    {
        isLoadingConsumptions = true;
        try
        {
            Consumptions.Clear();
            foreach (var consumption in consumptions.OrderByDescending(item => item.ConsumedAt))
            {
                Consumptions.Add(consumption);
            }
        }
        finally
        {
            isLoadingConsumptions = false;
        }

        SynchronizeConsumptionSubscriptions();
        RecalculateSourceStatistics();
        RecalculateDailyCaffeine(DateTimeOffset.Now);
    }

    private void InsertInChronologicalOrder(CaffeineConsumption consumption)
    {
        var index = 0;
        while (index < Consumptions.Count && Consumptions[index].ConsumedAt > consumption.ConsumedAt)
        {
            index++;
        }

        Consumptions.Insert(index, consumption);
    }

    private void ReorderConsumption(CaffeineConsumption consumption)
    {
        var existingIndex = Consumptions.IndexOf(consumption);
        if (existingIndex >= 0)
        {
            Consumptions.RemoveAt(existingIndex);
            InsertInChronologicalOrder(consumption);
        }
    }

    private void OnConsumptionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (isLoadingConsumptions)
        {
            return;
        }

        SynchronizeConsumptionSubscriptions();
        RecalculateSourceStatistics();
        RecalculateDailyCaffeine(DateTimeOffset.Now);
    }

    private void SynchronizeConsumptionSubscriptions()
    {
        foreach (var consumption in subscribedConsumptions)
        {
            consumption.PropertyChanged -= OnConsumptionPropertyChanged;
        }

        subscribedConsumptions.Clear();
        foreach (var consumption in Consumptions)
        {
            consumption.PropertyChanged += OnConsumptionPropertyChanged;
            subscribedConsumptions.Add(consumption);
        }
    }

    private void OnConsumptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CaffeineConsumption.Type))
        {
            RecalculateSourceStatistics();
        }

        if (e.PropertyName is nameof(CaffeineConsumption.CaffeineMg) or nameof(CaffeineConsumption.ConsumedAt))
        {
            RecalculateDailyCaffeine(DateTimeOffset.Now);
        }
    }

    private void RecalculateDailyCaffeine(DateTimeOffset now)
    {
        var localNow = now.ToLocalTime();
        var startOfToday = new DateTimeOffset(
            localNow.Date,
            TimeZoneInfo.Local.GetUtcOffset(localNow.Date));

        CurrentCaffeine = Consumptions
            .Where(consumption =>
                consumption.ConsumedAt.ToLocalTime() >= startOfToday &&
                consumption.ConsumedAt <= now)
            .Sum(consumption => Math.Max(0, consumption.CaffeineMg));
    }

    private async Task RunPeriodicUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await MainThread.InvokeOnMainThreadAsync(RefreshPeriodicData);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Нормальное завершение при уходе со страницы.
        }
    }

    private void RefreshPeriodicData()
    {
        foreach (var consumption in Consumptions)
        {
            consumption.RefreshRelativeTime();
        }

        var now = DateTimeOffset.Now;
        var today = now.ToLocalTime().Date;
        if (today != currentLocalDate)
        {
            currentLocalDate = today;
            RecalculateDailyCaffeine(now);
        }
    }

    private static Color GetResourceColor(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : fallback;

    private void RecalculateSourceStatistics()
    {
        var counts = Enum
            .GetValues<CaffeineConsumptionType>()
            .ToDictionary(type => type, _ => 0);

        foreach (var consumption in Consumptions)
        {
            counts[consumption.Type]++;
        }

        var totalCount = Consumptions.Count;
        CoffeeSource = new CaffeineSourceStat(
            "Кофе",
            counts[CaffeineConsumptionType.Coffee],
            totalCount,
            Color.FromArgb("#111111"));
        TeaSource = new CaffeineSourceStat(
            "Чай",
            counts[CaffeineConsumptionType.Tea],
            totalCount,
            Color.FromArgb("#B8B8B8"));
        EnergyDrinkSource = new CaffeineSourceStat(
            "Энергетики",
            counts[CaffeineConsumptionType.EnergyDrink],
            totalCount,
            Color.FromArgb("#686868"));

        SourceStats.Clear();
        SourceStats.Add(CoffeeSource);
        SourceStats.Add(TeaSource);
        SourceStats.Add(EnergyDrinkSource);
    }
}
