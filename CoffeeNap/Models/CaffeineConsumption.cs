using CommunityToolkit.Mvvm.ComponentModel;
using CoffeeNap.Helpers;
using SQLite;

namespace CoffeeNap.Models;

/// <summary>
/// Одна запись в истории употребления кофеина. Наследование от ObservableObject
/// позволяет автоматически обновлять привязанные элементы интерфейса.
/// </summary>
[Table("CaffeineConsumptions")]
public sealed class CaffeineConsumption : ObservableObject
{
    // Изменяемые свойства используют backing fields, чтобы SetProperty мог
    // сравнить значения и отправить уведомление только при реальном изменении.
    private DateTimeOffset consumedAt;
    private int caffeineMg;
    private CaffeineConsumptionType type;

    private string name = string.Empty;

    /// <summary>Уникальный ключ persistent-записи.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Отображаемое название напитка.</summary>
    [MaxLength(100)]
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    /// <summary>Момент употребления с информацией о смещении часового пояса.</summary>
    [Indexed(Name = "IX_CaffeineConsumptions_ConsumedAt")]
    public DateTimeOffset ConsumedAt
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
    public int CaffeineMg
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

    /// <summary>Имя изображения из Resources/Images, вычисляемое из типа.</summary>
    [Ignore]
    public string Icon => Type switch
    {
        CaffeineConsumptionType.EnergyDrink => "energy_drink_ico.png",
        CaffeineConsumptionType.Tea => "tea_ico.png",
        _ => "coffee_ico.png"
    };

    /// <summary>Цвет круглой подложки под иконкой, вычисляемый из типа.</summary>
    [Ignore]
    public Color IconBackground => Type switch
    {
        CaffeineConsumptionType.EnergyDrink => Color.FromArgb("#5C5C5C"),
        CaffeineConsumptionType.Tea => Color.FromArgb("#777777"),
        _ => Colors.Black
    };

    /// <summary>Категория напитка, используемая при группировке статистики.</summary>
    public CaffeineConsumptionType Type
    {
        get => type;
        set
        {
            if (SetProperty(ref type, value))
            {
                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(IconBackground));
            }
        }
    }

    /// <summary>Количество с единицей измерения для прямой привязки в XAML.</summary>
    [Ignore]
    public string CaffeineDisplay => $"{CaffeineMg} мг";

    /// <summary>Человекочитаемое время относительно текущего момента.</summary>
    [Ignore]
    public string RelativeTime => RelativeTimeFormatter.Format(ConsumedAt, DateTimeOffset.Now);

    /// <summary>
    /// Просит интерфейс заново прочитать <see cref="RelativeTime"/>, хотя сама
    /// запись не изменилась. Вызывается периодическим таймером MainViewModel.
    /// </summary>
    public void RefreshRelativeTime() => OnPropertyChanged(nameof(RelativeTime));
}
