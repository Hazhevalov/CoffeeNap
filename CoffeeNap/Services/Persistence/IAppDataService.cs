using CoffeeNap.Models;

namespace CoffeeNap.Services;

/// <summary>Shared asynchronous facade for all persistent application data.</summary>
public interface IAppDataService
{
    event EventHandler<CaffeineConsumption>? ConsumptionAdded;
    event EventHandler<CaffeineConsumption>? ConsumptionDeleted;
    event EventHandler? UserDataDeleted;

    // Initializes persistence and prepares required application data.
    Task InitializeAsync();

    // Loads the saved user profile.
    Task<UserProfile?> GetUserProfileAsync();
    // Saves the user profile.
    Task SaveUserProfileAsync(UserProfile profile);

    // Loads application settings.
    Task<AppSettings> GetSettingsAsync();
    // Saves application settings.
    Task SaveSettingsAsync(AppSettings settings);

    // Loads the consumption history.
    Task<IReadOnlyList<CaffeineConsumption>> GetConsumptionsAsync();
    // Loads a page of consumption records using the supplied cursor.
    Task<IReadOnlyList<CaffeineConsumption>> GetConsumptionsPageAsync(ConsumptionCursor? before, int pageSize);
    // Loads daily caffeine totals and consumption counts.
    Task<ConsumptionOverview> GetConsumptionOverviewAsync(DateTimeOffset now);
    // Loads consumption records within the specified time range.
    Task<IReadOnlyList<CaffeineConsumption>> GetConsumptionsBetweenAsync(
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive);
    // Validates and saves a consumption together with its recipe.
    Task AddConsumptionAndSaveRecipeAsync(
        CaffeineConsumption consumption,
        LastConsumptionRecipe recipe);
    // Loads the last saved consumption recipe.
    Task<LastConsumptionRecipe?> GetLastConsumptionRecipeAsync();
    // Deletes a consumption record by its identifier.
    Task DeleteConsumptionAsync(int id);
    // Deletes all stored user data.
    Task DeleteAllUserDataAsync();
}
