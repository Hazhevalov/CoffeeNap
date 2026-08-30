namespace CoffeeNap.Models;

/// <summary>
/// Поддерживаемые категории источников кофеина. Значения используются как ключи
/// статистики в MainPageViewModel и должны оставаться согласованными с диаграммой MainPage.
/// </summary>
public enum CaffeineConsumptionType
{
    Coffee,
    Tea,
    EnergyDrink
}
