namespace CoffeeNap.Models;

public sealed record CoffeeDrinkServingProfile(
    int SmallMl,
    int MediumMl,
    int LargeMl)
{
    public int GetVolumeMl(ServingSize servingSize) => servingSize switch
    {
        ServingSize.Small => SmallMl,
        ServingSize.Medium => MediumMl,
        ServingSize.Large => LargeMl,
        _ => throw new ArgumentOutOfRangeException(nameof(servingSize))
    };
}
