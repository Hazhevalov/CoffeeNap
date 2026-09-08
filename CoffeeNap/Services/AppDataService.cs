using CoffeeNap.Data;
using CoffeeNap.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.Services;

/// <summary>
/// Централизует инициализацию, legacy-миграцию, defaults и нормализацию данных
/// перед записью в SQLite.
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

    public AppDataService(
        AppDatabase database,
        ILogger<AppDataService> logger)
    {
        _database = database;
        _logger = logger;
    }

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

    public async Task<UserProfile?> GetUserProfileAsync()
    {
        await InitializeAsync();
        return await _database.GetUserProfileAsync();
    }

    public async Task SaveUserProfileAsync(UserProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        await InitializeAsync();

        profile.Id = DatabaseConstants.UserProfileId;
        profile.UserName = profile.UserName.Trim();
        await _database.SaveUserProfileAsync(profile);
    }

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

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await InitializeAsync();

        if (settings.DailyCaffeineLimit <= 0)
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

    public async Task<IReadOnlyList<CaffeineConsumption>> GetConsumptionsAsync()
    {
        await InitializeAsync();
        return await _database.GetConsumptionsAsync();
    }

    public async Task<IReadOnlyList<CaffeineConsumption>> GetConsumptionsForDateAsync(DateTime date)
    {
        var localStart = DateTime.SpecifyKind(date.Date, DateTimeKind.Local);
        var localEnd = localStart.AddDays(1);
        return await GetConsumptionsBetweenAsync(
            new DateTimeOffset(localStart).ToUniversalTime(),
            new DateTimeOffset(localEnd).ToUniversalTime());
    }

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

    public async Task AddConsumptionAsync(CaffeineConsumption consumption)
    {
        ValidateConsumption(consumption);
        await InitializeAsync();

        consumption.Id = 0;
        consumption.ConsumedAt = consumption.ConsumedAt.ToUniversalTime();
        await _database.InsertConsumptionAsync(consumption);
        ConsumptionAdded?.Invoke(this, consumption);
    }

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
        await _database.SaveConsumptionAndRecipeAsync(consumption, recipe);

        _lastRecipe = recipe;
        _isLastRecipeLoaded = true;
        ConsumptionAdded?.Invoke(this, consumption);
    }

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

    public async Task UpdateConsumptionAsync(CaffeineConsumption consumption)
    {
        ValidateConsumption(consumption);
        if (consumption.Id <= 0)
        {
            throw new ArgumentException("A persistent consumption must have a valid Id.", nameof(consumption));
        }

        await InitializeAsync();
        consumption.ConsumedAt = consumption.ConsumedAt.ToUniversalTime();
        var updatedRows = await _database.UpdateConsumptionAsync(consumption);
        if (updatedRows == 0)
        {
            throw new InvalidOperationException($"Consumption with Id {consumption.Id} was not found.");
        }
    }

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

        ConsumptionDeleted?.Invoke(this, consumption);
    }

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

        UserDataDeleted?.Invoke(this, EventArgs.Empty);
    }

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

        // Удаляем legacy user data только после успешной записи одной profile-row.
        Preferences.Default.Remove(LegacyUserNameKey);
        Preferences.Default.Remove(LegacyOnboardingCompletedKey);
        if (!string.IsNullOrEmpty(legacyName) || legacyOnboardingCompleted)
        {
            _logger.LogInformation("Legacy onboarding Preferences migrated to SQLite.");
        }
    }

    private async Task EnsureDefaultSettingsAsync()
    {
        if (await _database.GetSettingsAsync() is null)
        {
            await _database.SaveSettingsAsync(CreateDefaultSettings());
        }
    }

    private static AppSettings CreateDefaultSettings() => new()
    {
        Id = DatabaseConstants.SettingsId,
        LanguageCode = AppSettings.DefaultLanguageCode
    };

    private static void ValidateConsumption(CaffeineConsumption consumption)
    {
        ArgumentNullException.ThrowIfNull(consumption);

        consumption.Name = consumption.Name.Trim();
        if (string.IsNullOrEmpty(consumption.Name))
        {
            throw new ArgumentException("Consumption name cannot be empty.", nameof(consumption));
        }

        if (consumption.CaffeineMg < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(consumption), "Caffeine amount cannot be negative.");
        }

        if (consumption.ConsumedAt == default)
        {
            throw new ArgumentException("Consumption time is required.", nameof(consumption));
        }
    }

    private static void ValidateRecipe(LastConsumptionRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        var isValid = recipe.DrinkType switch
        {
            CaffeineConsumptionType.Coffee => recipe.CoffeeLocation switch
            {
                CoffeeLocation.Home =>
                    recipe.BrewingMethod is not null &&
                    recipe.CoffeeAmountGrams is > 0 &&
                    recipe.BeanType is not null,
                CoffeeLocation.Outside =>
                    recipe.CoffeeDrinkType is not null &&
                    recipe.VolumeMl is > 0 &&
                    recipe.BeanType is not null,
                _ => false
            },
            CaffeineConsumptionType.Tea =>
                recipe.TeaType is not null && recipe.TeaAmountGrams is > 0,
            CaffeineConsumptionType.EnergyDrink => recipe.EnergyDrinkVolumeMl is > 0,
            _ => false
        };

        if (!isValid)
        {
            throw new ArgumentException("The recipe is incomplete.", nameof(recipe));
        }
    }
}
