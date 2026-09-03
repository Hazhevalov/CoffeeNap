using CoffeeNap.Models;

namespace CoffeeNap.Services;

/// <summary>Reusable count-based distribution calculator for MainPage and CalendarPage.</summary>
public static class ConsumptionTypeDistributionCalculator
{
    public static ConsumptionTypeDistribution Calculate(
        IEnumerable<CaffeineConsumption> consumptions)
    {
        var coffee = 0;
        var tea = 0;
        var energyDrink = 0;

        foreach (var consumption in consumptions)
        {
            switch (consumption.Type)
            {
                case CaffeineConsumptionType.Coffee:
                    coffee++;
                    break;
                case CaffeineConsumptionType.Tea:
                    tea++;
                    break;
                case CaffeineConsumptionType.EnergyDrink:
                    energyDrink++;
                    break;
            }
        }

        return new ConsumptionTypeDistribution(coffee, tea, energyDrink);
    }
}
