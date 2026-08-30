using SQLite;

namespace CoffeeNap.Models;

// Настройки приложения
[Table("AppSettings")]
public sealed class AppSettings
{
    public const double DefaultDailyCaffeineLimit = 300;

    [PrimaryKey]
    public int Id { get; set; }

    public double DailyCaffeineLimit { get; set; } = DefaultDailyCaffeineLimit;
}
