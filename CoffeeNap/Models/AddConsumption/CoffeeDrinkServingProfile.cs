namespace CoffeeNap.Models;

// Stores the standard serving volumes for a coffee drink.
public sealed record CoffeeDrinkServingProfile(
    int SmallMl,
    int MediumMl,
    int LargeMl)
{
    // Returns the volume for the selected serving size.
    public int GetVolumeMl(ServingSize servingSize) => servingSize switch
    {
        ServingSize.Small => SmallMl,
        ServingSize.Medium => MediumMl,
        ServingSize.Large => LargeMl,
        _ => throw new ArgumentOutOfRangeException(nameof(servingSize))
    };
}
