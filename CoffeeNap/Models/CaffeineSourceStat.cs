namespace CoffeeNap.Models;

/// <summary>
/// Неизменяемый снимок статистики одной категории напитков. Подготавливает
/// значения в формате, который можно напрямую привязать к сегменту диаграммы.
/// </summary>
public sealed class CaffeineSourceStat
{
    /// <summary>Создаёт сегмент статистики и нормализует отрицательные счётчики.</summary>
    public CaffeineSourceStat(string name, int count, int totalCount, Color color)
    {
        Name = name;
        Count = Math.Max(0, count);
        TotalCount = Math.Max(0, totalCount);
        Color = color;
    }

    /// <summary>Название категории в легенде.</summary>
    public string Name { get; }

    /// <summary>Количество записей этой категории.</summary>
    public int Count { get; }

    /// <summary>Общее количество записей всех категорий.</summary>
    public int TotalCount { get; }

    /// <summary>Цвет сегмента и маркера легенды.</summary>
    public Color Color { get; }

    /// <summary>Доля категории от общего количества в диапазоне от 0 до 1.</summary>
    public double Ratio => TotalCount == 0 ? 0 : (double)Count / TotalCount;

    /// <summary>Доля в локализованном процентном формате без дробной части.</summary>
    public string PercentageDisplay => $"{Ratio:P0}";

    // В слишком узком сегменте процент не поместится, поэтому скрываем подпись.
    public bool IsPercentageVisible => Ratio >= 0.08;

    // Star-ширина превращает долю источника в пропорциональный сегмент диаграммы.
    public GridLength SegmentWidth => Ratio == 0
        ? new GridLength(0)
        : new GridLength(Ratio, GridUnitType.Star);
}
