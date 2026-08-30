using SQLite;

namespace CoffeeNap.Models;

// Класс употребления 
[Table("CaffeineConsumptions")]
public sealed class CaffeineConsumption
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Indexed(Name = "IX_CaffeineConsumptions_ConsumedAt")]
    public DateTimeOffset ConsumedAt { get; set; }

    public int CaffeineMg { get; set; }

    public CaffeineConsumptionType Type { get; set; }
}
