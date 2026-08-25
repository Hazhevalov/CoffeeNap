using SQLite;

namespace CoffeeNap.Models;

/// <summary>Расширяемый набор пользовательских настроек приложения.</summary>
[Table("AppSettings")]
public sealed class AppSettings
{
    public const double DefaultDailyCaffeineLimit = 300;

    [PrimaryKey]
    public int Id { get; set; }

    public double DailyCaffeineLimit { get; set; } = DefaultDailyCaffeineLimit;
}
