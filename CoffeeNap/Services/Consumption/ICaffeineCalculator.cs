using CoffeeNap.Models;

namespace CoffeeNap.Services;

public interface ICaffeineCalculator
{
    // Calculates the caffeine result from the current quiz answers.
    ConsumptionCalculationResult Calculate(AddConsumptionQuizState state);
}
