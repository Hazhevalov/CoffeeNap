using CoffeeNap.Models;

namespace CoffeeNap.Services;

public interface ICaffeineCalculator
{
    ConsumptionCalculationResult Calculate(AddConsumptionQuizState state);
}
