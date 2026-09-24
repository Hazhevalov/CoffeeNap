namespace CoffeeNap.Models;

// Stores consumption counts for each drink category.
public readonly record struct ConsumptionTypeDistribution(
    int CoffeeCount,
    int TeaCount,
    int EnergyDrinkCount)
{
    public int TotalCount => CoffeeCount + TeaCount + EnergyDrinkCount;
    public double CoffeeRatio => GetRatio(CoffeeCount);
    public double TeaRatio => GetRatio(TeaCount);
    public double EnergyDrinkRatio => GetRatio(EnergyDrinkCount);

    // Returns a drink category's share of total consumption.
    private double GetRatio(int count) => TotalCount == 0 ? 0 : (double)count / TotalCount;
}
