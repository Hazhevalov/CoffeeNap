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
    private double currentCaffeine = 120;
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
        set
        {
            if (SetProperty(ref currentCaffeine, value))
            {
                OnPropertyChanged(nameof(DailyProgress));
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

    public ObservableCollection<CaffeineSourceStat> SourceStats { get; }

    public ObservableCollection<CaffeineConsumption> Consumptions { get; }

    [RelayCommand]
    private Task OpenSettingsAsync() => Shell.Current.GoToAsync(nameof(SettingsPage));

    [RelayCommand]
    private Task OpenAddConsumptionAsync() => Shell.Current.GoToAsync(nameof(AddConsumptionPage));

    [RelayCommand]
    private Task OpenCalendarAsync() => Shell.Current.GoToAsync(nameof(CalendarPage));

    private void LoadConsumptions()
    {
        // Временные данные для демонстрации интерфейса. Позже этот массив можно
        // заменить загрузкой из базы данных или внешнего сервиса.
        var now = DateTimeOffset.Now;
        var testConsumptions = new[]
        {
            CreateConsumption("Латте на кокосовом", 120, now.AddMinutes(-15), CaffeineConsumptionType.Coffee),
            CreateConsumption("Эспрессо", 65, now.AddHours(-2), CaffeineConsumptionType.Coffee),
            CreateConsumption("Капучино", 80, now.AddHours(-5), CaffeineConsumptionType.Coffee),
            CreateConsumption("Американо", 95, now.AddDays(-1), CaffeineConsumptionType.Coffee),
            CreateConsumption("Флэт уайт", 110, now.AddDays(-2), CaffeineConsumptionType.Coffee),
            CreateConsumption("Зелёный чай", 35, now.AddDays(-3), CaffeineConsumptionType.Tea),
            CreateConsumption("Энергетик #1", 80, now.AddDays(-4), CaffeineConsumptionType.EnergyDrink),
            CreateConsumption("Энергетик #2", 100, now.AddDays(-5), CaffeineConsumptionType.EnergyDrink),
            CreateConsumption("Энергетик #3", 120, now.AddDays(-6), CaffeineConsumptionType.EnergyDrink),
            CreateConsumption("Энергетик (500мл)", 160, now.AddDays(-7), CaffeineConsumptionType.EnergyDrink)
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
        // На распределение по источникам влияет только тип напитка.
        if (e.PropertyName == nameof(CaffeineConsumption.Type))
        {
            RecalculateSourceStatistics();
        }
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
