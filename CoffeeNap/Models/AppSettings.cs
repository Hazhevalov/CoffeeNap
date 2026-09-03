using SQLite;

namespace CoffeeNap.Models;

// Настройки приложения
[Table("AppSettings")]
public sealed class AppSettings
{
    public const double DefaultDailyCaffeineLimit = 300;
    public const string DefaultLanguageCode = "en";

    [PrimaryKey]
    public int Id { get; set; }

    public double DailyCaffeineLimit { get; set; } = DefaultDailyCaffeineLimit;

    [MaxLength(5)]
    public string LanguageCode { get; set; } = DefaultLanguageCode;
}
