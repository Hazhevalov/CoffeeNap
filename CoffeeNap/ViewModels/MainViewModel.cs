using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CoffeeNap.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CoffeeNap.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // Храним подписанные элементы отдельно, чтобы при изменении коллекции
    // можно было безопасно отписаться от старых обработчиков событий.
    private readonly HashSet<CaffeineConsumption> subscribedConsumptions = [];
    private CancellationTokenSource? periodicUpdateCancellation;
    private DateTime currentLocalDate = DateTime.Today;
    private double currentCaffeine;
    private double dailyCaffeineLimit = 300;
    private CaffeineSourceStat coffeeSource = null!;
    private CaffeineSourceStat teaSource = null!;
    private CaffeineSourceStat energyDrinkSource = null!;
    private bool isLoadingConsumptions;

    public MainViewModel()
    {
        SourceStats = new ObservableCollection<CaffeineSourceStat>();
        Consumptions = new ObservableCollection<CaffeineConsumption>();
        Consumptions.CollectionChanged += OnConsumptionsCollectionChanged;

        LoadConsumptions();
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
        set
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

    private void LoadConsumptions()
    {
        // Временные данные для демонстрации интерфейса. Позже этот массив можно
        // заменить загрузкой из базы данных или внешнего сервиса.
        var now = DateTimeOffset.Now;
        var yesterday = now.AddDays(-1);
        var testConsumptions = new[]
        {
            CreateConsumption("Капучино", 800, GetTodayTimestamp(now, TimeSpan.FromMinutes(5)), CaffeineConsumptionType.Coffee),
            CreateConsumption("Энергетик (500мл)", 160, GetTodayTimestamp(now, TimeSpan.FromMinutes(58)), CaffeineConsumptionType.EnergyDrink),
            CreateConsumption("Эспрессо", 65, yesterday, CaffeineConsumptionType.Coffee),
            CreateConsumption("Американо", 95, now.AddDays(-2), CaffeineConsumptionType.Coffee),
            CreateConsumption("Флэт уайт", 110, now.AddDays(-3), CaffeineConsumptionType.Coffee),
            CreateConsumption("Латте на кокосовом", 120, now.AddDays(-4), CaffeineConsumptionType.Coffee),
            CreateConsumption("Зелёный чай", 35, now.AddDays(-5), CaffeineConsumptionType.Tea),
            CreateConsumption("Энергетик #1", 80, now.AddDays(-6), CaffeineConsumptionType.EnergyDrink),
            CreateConsumption("Энергетик #2", 100, now.AddDays(-7), CaffeineConsumptionType.EnergyDrink),
            CreateConsumption("Энергетик #3", 120, now.AddDays(-8), CaffeineConsumptionType.EnergyDrink)
        };

        // Во время пакетного заполнения не пересчитываем статистику после
        // каждого Add: одного пересчёта в конце загрузки достаточно.
        isLoadingConsumptions = true;
        try
        {
            foreach (var consumption in testConsumptions)
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
        RecalculateDailyCaffeine(now);
    }

    private static DateTimeOffset GetTodayTimestamp(DateTimeOffset now, TimeSpan age)
    {
        var localNow = now.ToLocalTime();
        var startOfToday = new DateTimeOffset(
            localNow.Date,
            TimeZoneInfo.Local.GetUtcOffset(localNow.Date));
        var requested = localNow - age;
        return requested >= startOfToday ? requested : startOfToday;
    }

    private static CaffeineConsumption CreateConsumption(
        string name,
        int caffeineMg,
        DateTimeOffset consumedAt,
        CaffeineConsumptionType type)
    {
        var isEnergyDrink = type == CaffeineConsumptionType.EnergyDrink;
        var isTea = type == CaffeineConsumptionType.Tea;

        return new CaffeineConsumption
        {
            Name = name,
            CaffeineMg = caffeineMg,
            ConsumedAt = consumedAt,
            Type = type,
            Icon = isEnergyDrink
                ? "energy_drink_ico.png"
                : isTea
                    ? "tea_ico.png"
                    : "coffee_ico.png",
            IconBackground = isEnergyDrink
                ? Color.FromArgb("#5C5C5C")
                : isTea
                    ? Color.FromArgb("#777777")
                    : Colors.Black
        };
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
        // ObservableCollection сообщает о добавлении и удалении элементов,
        // но не об изменении их свойств. Поэтому подписываемся на каждый элемент.
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

    private static Color GetResourceColor(string key, Color fallback)
    {
        return Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : fallback;
    }

    private void RecalculateSourceStatistics()
    {
        // Создаём запись для каждого значения enum заранее, включая типы,
        // которых пока нет в истории употреблений.
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
            Color.FromArgb("#686868"));
        EnergyDrinkSource = new CaffeineSourceStat(
            "Энергетики",
            counts[CaffeineConsumptionType.EnergyDrink],
            totalCount,
            Color.FromArgb("#B8B8B8"));

        SourceStats.Clear();
        SourceStats.Add(CoffeeSource);
        SourceStats.Add(TeaSource);
        SourceStats.Add(EnergyDrinkSource);
    }
}
