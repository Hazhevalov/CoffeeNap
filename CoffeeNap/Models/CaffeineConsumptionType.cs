namespace CoffeeNap.Models;

/// <summary>
/// Поддерживаемые категории источников кофеина. Значения используются как ключи
/// статистики в MainPageViewModel и должны оставаться согласованными с диаграммой MainPage.
/// </summary>
public enum CaffeineConsumptionType
{
    /// <summary>Кофейный напиток.</summary>
    Coffee,

    /// <summary>Чай.</summary>
    Tea,

    /// <summary>Энергетический напиток.</summary>
    EnergyDrink
}
