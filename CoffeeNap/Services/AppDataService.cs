using System.Diagnostics;
using CoffeeNap.Data;
using CoffeeNap.Models;

namespace CoffeeNap.Services;

/// <summary>
/// Централизует инициализацию, legacy-миграцию, defaults и нормализацию данных
/// перед записью в SQLite.
/// </summary>
public sealed class AppDataService(AppDatabase database) : IAppDataService
{
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private bool isInitialized;

    public async Task InitializeAsync()
    {
        if (isInitialized)
        {
            return;
        }

        await initializationLock.WaitAsync();
        try
        {
            if (isInitialized)
            {
                return;
            }

            await database.InitializeAsync();
            await MigrateLegacyPreferencesAsync();
            await EnsureDefaultSettingsAsync();
            isInitialized = true;
        }
        finally
        {
            initializationLock.Release();
        }
    }

    public async Task<UserProfile?> GetUserProfileAsync()
    {
        await InitializeAsync();
        return await database.GetUserProfileAsync();
    }

    public async Task SaveUserProfileAsync(UserProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        await InitializeAsync();

        profile.Id = UserProfile.SingletonId;
        profile.UserName = profile.UserName.Trim();
        await database.SaveUserProfileAsync(profile);
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        await InitializeAsync();
        return await database.GetSettingsAsync() ?? CreateDefaultSettings();
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

        settings.Id = AppSettings.SingletonId;
        await database.SaveSettingsAsync(settings);
    }

    public async Task<IReadOnlyList<CaffeineConsumption>> GetConsumptionsAsync()
    {
        await InitializeAsync();
        return await database.GetConsumptionsAsync();
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

        await InitializeAsync();
        return await database.GetConsumptionsBetweenAsync(
            fromInclusive.ToUniversalTime(),
            toExclusive.ToUniversalTime());
    }

    public async Task AddConsumptionAsync(CaffeineConsumption consumption)
    {
        ValidateConsumption(consumption);
        await InitializeAsync();

        consumption.Id = 0;
        consumption.ConsumedAt = consumption.ConsumedAt.ToUniversalTime();
        await database.InsertConsumptionAsync(consumption);
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
        var updatedRows = await database.UpdateConsumptionAsync(consumption);
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
        await database.DeleteConsumptionAsync(id);
    }

    private async Task MigrateLegacyPreferencesAsync()
    {
        if (await database.GetUserProfileAsync() is not null)
        {
            return;
        }

        var legacyName = Preferences.Default
            .Get(PreferenceKeys.UserName, string.Empty)
            .Trim();
        var legacyOnboardingCompleted = Preferences.Default
            .Get(PreferenceKeys.OnboardingCompleted, false);

        var profile = new UserProfile
        {
            UserName = legacyName,
            OnboardingCompleted = legacyOnboardingCompleted && !string.IsNullOrEmpty(legacyName)
        };

        await database.SaveUserProfileAsync(profile);

        // Удаляем legacy user data только после успешной записи одной profile-row.
        Preferences.Default.Remove(PreferenceKeys.UserName);
        Preferences.Default.Remove(PreferenceKeys.OnboardingCompleted);
#if DEBUG
        if (!string.IsNullOrEmpty(legacyName) || legacyOnboardingCompleted)
        {
            Debug.WriteLine("Legacy onboarding Preferences migrated to SQLite.");
        }
#endif
    }

    private async Task EnsureDefaultSettingsAsync()
    {
        if (await database.GetSettingsAsync() is null)
        {
            await database.SaveSettingsAsync(CreateDefaultSettings());
        }
    }

    private static AppSettings CreateDefaultSettings() => new();

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
}
