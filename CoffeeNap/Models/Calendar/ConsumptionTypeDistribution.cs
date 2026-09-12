namespace CoffeeNap.Models;

public readonly record struct ConsumptionTypeDistribution(
    int CoffeeCount,
    int TeaCount,
    int EnergyDrinkCount)
{
    public int TotalCount => CoffeeCount + TeaCount + EnergyDrinkCount;
    public double CoffeeRatio => GetRatio(CoffeeCount);
    public double TeaRatio => GetRatio(TeaCount);
    public double EnergyDrinkRatio => GetRatio(EnergyDrinkCount);

    private double GetRatio(int count) => TotalCount == 0 ? 0 : (double)count / TotalCount;
}
