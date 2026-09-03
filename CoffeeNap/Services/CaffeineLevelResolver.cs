using CoffeeNap.Models;

namespace CoffeeNap.Services;

/// <summary>Single source of caffeine thresholds used throughout the application.</summary>
public static class CaffeineLevelResolver
{
    public const int MediumMinimumMg = 150;
    public const int HighMinimumMg = 280;
    public const int LimitExceededMinimumMg = 300;

    public const double LowProgressMaximum = 0.40;
    public const double MediumProgressMaximum = 0.70;
    public const double LimitProgressMinimum = 1.00;

    public static CaffeineLevel ResolveDailyTotal(int totalCaffeineMg) => totalCaffeineMg switch
    {
        <= 0 => CaffeineLevel.None,
        < MediumMinimumMg => CaffeineLevel.Low,
        < HighMinimumMg => CaffeineLevel.Medium,
        < LimitExceededMinimumMg => CaffeineLevel.High,
        _ => CaffeineLevel.LimitExceeded
    };

    public static CaffeineLevel ResolveProgress(double ratio) => ratio switch
    {
        <= LowProgressMaximum => CaffeineLevel.Low,
        <= MediumProgressMaximum => CaffeineLevel.Medium,
        < LimitProgressMinimum => CaffeineLevel.High,
        _ => CaffeineLevel.LimitExceeded
    };
}
