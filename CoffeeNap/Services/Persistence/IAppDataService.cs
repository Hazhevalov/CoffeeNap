using CoffeeNap.Models;

namespace CoffeeNap.Services;

/// <summary>Единый async-фасад для всех persistent-данных приложения.</summary>
public interface IAppDataService
{
    event EventHandler<CaffeineConsumption>? ConsumptionAdded;
    event EventHandler<CaffeineConsumption>? ConsumptionDeleted;
    event EventHandler? UserDataDeleted;

    Task InitializeAsync();

    Task<UserProfile?> GetUserProfileAsync();
    Task SaveUserProfileAsync(UserProfile profile);

    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);

    Task<IReadOnlyList<CaffeineConsumption>> GetConsumptionsAsync();
    Task<IReadOnlyList<CaffeineConsumption>> GetConsumptionsPageAsync(ConsumptionCursor? before, int pageSize);
    Task<ConsumptionOverview> GetConsumptionOverviewAsync(DateTimeOffset now);
    Task<IReadOnlyList<CaffeineConsumption>> GetConsumptionsBetweenAsync(
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive);
    Task AddConsumptionAndSaveRecipeAsync(
        CaffeineConsumption consumption,
        LastConsumptionRecipe recipe);
    Task<LastConsumptionRecipe?> GetLastConsumptionRecipeAsync();
    Task DeleteConsumptionAsync(int id);
    Task DeleteAllUserDataAsync();
}
