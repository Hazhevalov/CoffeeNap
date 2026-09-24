using CoffeeNap.Data;
using CoffeeNap.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.Services;

/// <summary>
/// Centralizes initialization, legacy migration, defaults, and data normalization
/// before writing to SQLite.
/// </summary>
public sealed class AppDataService : IAppDataService
{
    public event EventHandler<CaffeineConsumption>? ConsumptionAdded;
    public event EventHandler<CaffeineConsumption>? ConsumptionDeleted;
    public event EventHandler? UserDataDeleted;

    private const string LegacyUserNameKey = "coffee_nap.user_name";
    private const string LegacyOnboardingCompletedKey = "coffee_nap.onboarding_completed";
    private static readonly string[] LegacyResetKeys =
    [
        LegacyUserNameKey,
        LegacyOnboardingCompletedKey,
        "coffee_nap.language",
        "Language"
    ];
    private readonly AppDatabase _database;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly SemaphoreSlim _lastRecipeLock = new(1, 1);
    private readonly ILogger<AppDataService> _logger;
    private bool _isInitialized;
    private bool _isLastRecipeLoaded;
    private LastConsumptionRecipe? _lastRecipe;

    // Initializes the app data service.
    public AppDataService(
        AppDatabase database,
        ILogger<AppDataService> logger)
    {
        _database = database;
        _logger = logger;
    }

    // Initializes persistence and prepares required application data.
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await _initializationLock.WaitAsync();
        try
        {
            if (_isInitialized)
            {
                return;
            }

            await _database.InitializeAsync();
            await MigrateLegacyPreferencesAsync();
            await EnsureDefaultSettingsAsync();
            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    // Loads the saved user profile.
    public async Task<UserProfile?> GetUserProfileAsync()
    {
        await InitializeAsync();
        return await _database.GetUserProfileAsync();
    }

    // Saves the user profile.
    public async Task SaveUserProfileAsync(UserProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        await InitializeAsync();

        profile.Id = DatabaseConstants.UserProfileId;
        profile.UserName = profile.UserName.Trim();
        await _database.SaveUserProfileAsync(profile);
    }

    // Loads application settings.
    public async Task<AppSettings> GetSettingsAsync()
    {
        await InitializeAsync();
        var settings = await _database.GetSettingsAsync() ?? CreateDefaultSettings();
        if (settings.LanguageCode is not ("ru" or "en"))
        {
            settings.LanguageCode = AppSettings.DefaultLanguageCode;
            await _database.SaveSettingsAsync(settings);
        }

        return settings;
    }

    // Saves application settings.
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await InitializeAsync();

        if (!double.IsFinite(settings.DailyCaffeineLimit) || settings.DailyCaffeineLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                "Daily caffeine limit must be greater than zero.");
        }

        if (settings.LanguageCode is not ("ru" or "en"))
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                "Language code must be either 'ru' or 'en'.");
        }

        settings.Id = DatabaseConstants.SettingsId;
        await _database.SaveSettingsAsync(settings);
    }

    // Loads the consumption history.
    public async Task<IReadOnlyList<CaffeineConsumption>> GetConsumptionsAsync()
    {
        await InitializeAsync();
        return await _database.GetConsumptionsAsync();
    }

    // Loads a page of consumption records using the supplied cursor.
    public async Task<IReadOnlyList<CaffeineConsumption>> GetConsumptionsPageAsync(ConsumptionCursor? before, int pageSize)
    {
        if (pageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(pageSize));
        await InitializeAsync();
        return await _database.GetConsumptionsPageAsync(before, pageSize);
    }

    // Loads daily caffeine totals and consumption counts.
    public async Task<ConsumptionOverview> GetConsumptionOverviewAsync(DateTimeOffset now)
    {
        await InitializeAsync();
        return await _database.GetConsumptionOverviewAsync(now);
    }

    // Loads consumption records within the specified time range.
    public async Task<IReadOnlyList<CaffeineConsumption>> GetConsumptionsBetweenAsync(
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive)
    {
        if (toExclusive <= fromInclusive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(toExclusive),
                "The end of the range must be later than the start.");
        }

        await InitializeAsync().ConfigureAwait(false);
        return await _database.GetConsumptionsBetweenAsync(
            fromInclusive.ToUniversalTime(),
            toExclusive.ToUniversalTime()).ConfigureAwait(false);
    }

    // Validates and saves a consumption together with its recipe.
    public async Task AddConsumptionAndSaveRecipeAsync(
        CaffeineConsumption consumption,
        LastConsumptionRecipe recipe)
    {
        ValidateConsumption(consumption);
        ValidateRecipe(recipe);
        await InitializeAsync();

        consumption.Id = 0;
        consumption.ConsumedAt = consumption.ConsumedAt.ToUniversalTime();
        recipe.Id = DatabaseConstants.LastConsumptionRecipeId;
        consumption.NameKey = ConsumptionNameKey.FromRecipe(recipe);
        await _database.SaveConsumptionAndRecipeAsync(consumption, recipe);

        _lastRecipe = recipe;
        _isLastRecipeLoaded = true;
        PublishSafely(ConsumptionAdded, consumption);
    }

    // Loads the last saved consumption recipe.
    public async Task<LastConsumptionRecipe?> GetLastConsumptionRecipeAsync()
    {
        await InitializeAsync();
        if (_isLastRecipeLoaded)
        {
            return _lastRecipe;
        }

        await _lastRecipeLock.WaitAsync();
        try
        {
            if (!_isLastRecipeLoaded)
            {
                _lastRecipe = await _database.GetLastConsumptionRecipeAsync();
                _isLastRecipeLoaded = true;
            }

            return _lastRecipe;
        }
        finally
        {
            _lastRecipeLock.Release();
        }
    }

    // Deletes a consumption record by its identifier.
    public async Task DeleteConsumptionAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        await InitializeAsync();
        var consumption = await _database.GetConsumptionAsync(id) ??
            throw new InvalidOperationException($"Consumption with Id {id} was not found.");
        var deletedRows = await _database.DeleteConsumptionAsync(id);
        if (deletedRows == 0)
        {
            throw new InvalidOperationException($"Consumption with Id {id} was not deleted.");
        }

        PublishSafely(ConsumptionDeleted, consumption);
    }

    // Deletes all stored user data.
    public async Task DeleteAllUserDataAsync()
    {
        await InitializeAsync();
        await _database.DeleteAllUserDataAsync();
        _lastRecipe = null;
        _isLastRecipeLoaded = true;

        foreach (var key in LegacyResetKeys)
        {
            Preferences.Default.Remove(key);
        }

        if (UserDataDeleted is { } handlers)
        {
            foreach (EventHandler handler in handlers.GetInvocationList())
            {
                try { handler(this, EventArgs.Empty); }
                catch (Exception exception) { _logger.LogError(exception, "User data reset subscriber failed after commit."); }
            }
        }
    }

    // Notifies consumption subscribers while isolating handler failures.
    private void PublishSafely(EventHandler<CaffeineConsumption>? handlers, CaffeineConsumption consumption)
    {
        if (handlers is null) return;
        foreach (EventHandler<CaffeineConsumption> handler in handlers.GetInvocationList())
        {
            try { handler(this, consumption); }
            catch (Exception exception) { _logger.LogError(exception, "Consumption subscriber failed after commit."); }
        }
    }

    // Migrates legacy preferences into the database.
    private async Task MigrateLegacyPreferencesAsync()
    {
        if (await _database.GetUserProfileAsync() is not null)
        {
            return;
        }

        var legacyName = Preferences.Default
            .Get(LegacyUserNameKey, string.Empty)
            .Trim();
        var legacyOnboardingCompleted = Preferences.Default
            .Get(LegacyOnboardingCompletedKey, false);

        var profile = new UserProfile
        {
            Id = DatabaseConstants.UserProfileId,
            UserName = legacyName,
            OnboardingCompleted = legacyOnboardingCompleted && !string.IsNullOrEmpty(legacyName)
        };

        await _database.SaveUserProfileAsync(profile);

        // Remove legacy user data only after successfully saving the profile row.
        Preferences.Default.Remove(LegacyUserNameKey);
        Preferences.Default.Remove(LegacyOnboardingCompletedKey);
        if (!string.IsNullOrEmpty(legacyName) || legacyOnboardingCompleted)
        {
            _logger.LogInformation("Legacy onboarding Preferences migrated to SQLite.");
        }
    }

    // Creates settings when no saved settings exist.
    private async Task EnsureDefaultSettingsAsync()
    {
        if (await _database.GetSettingsAsync() is null)
        {
            await _database.SaveSettingsAsync(CreateDefaultSettings());
        }
    }

    // Creates the default application settings.
    private static AppSettings CreateDefaultSettings() => new()
    {
        Id = DatabaseConstants.SettingsId,
        LanguageCode = AppSettings.DefaultLanguageCode
    };

    // Validates a consumption before saving it.
    private static void ValidateConsumption(CaffeineConsumption consumption)
    {
        ArgumentNullException.ThrowIfNull(consumption);

        consumption.Name = consumption.Name.Trim();
        if (string.IsNullOrEmpty(consumption.Name))
        {
            throw new ArgumentException("Consumption name cannot be empty.", nameof(consumption));
        }

        if (consumption.CaffeineMg is < 0 or > ConsumptionRecipeValidator.MaximumCaffeineMg)
        {
            throw new ArgumentOutOfRangeException(nameof(consumption), "Caffeine amount cannot be negative.");
        }

        if (consumption.ConsumedAt == default)
        {
            throw new ArgumentException("Consumption time is required.", nameof(consumption));
        }
    }

    // Validates a recipe before saving it.
    private static void ValidateRecipe(LastConsumptionRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        if (!ConsumptionRecipeValidator.IsValid(recipe))
        {
            throw new ArgumentException("The recipe is incomplete.", nameof(recipe));
        }
    }
}
