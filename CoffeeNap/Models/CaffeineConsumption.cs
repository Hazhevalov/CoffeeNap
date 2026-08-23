using CommunityToolkit.Mvvm.ComponentModel;
using CoffeeNap.Helpers;

namespace CoffeeNap.Models;

/// <summary>
/// Одна запись в истории употребления кофеина. Наследование от ObservableObject
/// позволяет автоматически обновлять привязанные элементы интерфейса.
/// </summary>
public sealed class CaffeineConsumption : ObservableObject
{
    // Изменяемые свойства используют backing fields, чтобы SetProperty мог
    // сравнить значения и отправить уведомление только при реальном изменении.
    private DateTimeOffset consumedAt;
    private int caffeineMg;
    private CaffeineConsumptionType type;

    /// <summary>Отображаемое название напитка.</summary>
    public required string Name { get; init; }

    /// <summary>Момент употребления с информацией о смещении часового пояса.</summary>
    public required DateTimeOffset ConsumedAt
    {
        get => consumedAt;
        set
        {
            if (SetProperty(ref consumedAt, value))
            {
                // Текст «N минут назад» зависит от времени записи.
                OnPropertyChanged(nameof(RelativeTime));
            }
        }
    }

    /// <summary>Количество кофеина в миллиграммах.</summary>
    public required int CaffeineMg
    {
        get => caffeineMg;
        set
        {
            if (SetProperty(ref caffeineMg, value))
            {
                // Обновляем готовую для UI строку вместе с числом.
                OnPropertyChanged(nameof(CaffeineDisplay));
            }
        }
    }

    /// <summary>Имя изображения из Resources/Images.</summary>
    public required string Icon { get; init; }

    /// <summary>Цвет круглой подложки под иконкой.</summary>
    public required Color IconBackground { get; init; }

    /// <summary>Категория напитка, используемая при группировке статистики.</summary>
    public required CaffeineConsumptionType Type
    {
        get => type;
        set => SetProperty(ref type, value);
    }

    /// <summary>Количество с единицей измерения для прямой привязки в XAML.</summary>
    public string CaffeineDisplay => $"{CaffeineMg} мг";

    /// <summary>Человекочитаемое время относительно текущего момента.</summary>
    public string RelativeTime => RelativeTimeFormatter.Format(ConsumedAt, DateTimeOffset.Now);

    /// <summary>
    /// Просит интерфейс заново прочитать <see cref="RelativeTime"/>, хотя сама
    /// запись не изменилась. Вызывается периодическим таймером MainViewModel.
    /// </summary>
    public void RefreshRelativeTime() => OnPropertyChanged(nameof(RelativeTime));
}
