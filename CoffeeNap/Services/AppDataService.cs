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
    private const string LegacyUserNameKey = "coffee_nap.user_name";
    private const string LegacyOnboardingCompletedKey = "coffee_nap.onboarding_completed";
    private readonly AppDatabase _database;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly ILogger<AppDataService> _logger;
    private bool _isInitialized;

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
#if DEBUG
            await EnsureTestConsumptionsAsync();
#endif
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
        return await _database.GetSettingsAsync() ?? CreateDefaultSettings();
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

        await InitializeAsync();
        return await _database.GetConsumptionsBetweenAsync(
            fromInclusive.ToUniversalTime(),
            toExclusive.ToUniversalTime());
    }

    public async Task AddConsumptionAsync(CaffeineConsumption consumption)
    {
        ValidateConsumption(consumption);
        await InitializeAsync();

        consumption.Id = 0;
        consumption.ConsumedAt = consumption.ConsumedAt.ToUniversalTime();
        await _database.InsertConsumptionAsync(consumption);
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
        await _database.DeleteConsumptionAsync(id);
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
// TEST TILL 266
#if DEBUG
    private async Task EnsureTestConsumptionsAsync()
    {
        // Seed выполняется только для совершенно пустой истории: существующие
        // пользовательские записи не изменяются, а повторный запуск не создаёт дубликаты.
        if ((await _database.GetConsumptionsAsync()).Count != 0)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        var testConsumptions = new[]
        {
            CreateTestConsumption(
                "Капучино",
                80,
                GetTodayTimestamp(now, TimeSpan.FromMinutes(15)),
                CaffeineConsumptionType.Coffee),
            CreateTestConsumption(
                "Энергетик (500мл)",
                160,
                GetTodayTimestamp(now, TimeSpan.FromMinutes(5)),
                CaffeineConsumptionType.EnergyDrink),
            CreateTestConsumption("Эспрессо", 65, now.AddDays(-1), CaffeineConsumptionType.Coffee),
            CreateTestConsumption("Американо", 95, now.AddDays(-2), CaffeineConsumptionType.Coffee),
            CreateTestConsumption("Флэт уайт", 110, now.AddDays(-3), CaffeineConsumptionType.Coffee),
            CreateTestConsumption("Латте на кокосовом", 120, now.AddDays(-4), CaffeineConsumptionType.Coffee),
            CreateTestConsumption("Зелёный чай", 35, now.AddDays(-5), CaffeineConsumptionType.Tea),
            CreateTestConsumption("Энергетик #1", 80, now.AddDays(-6), CaffeineConsumptionType.EnergyDrink),
            CreateTestConsumption("Энергетик #2", 100, now.AddDays(-7), CaffeineConsumptionType.EnergyDrink),
            CreateTestConsumption("Энергетик #3", 120, now.AddDays(-8), CaffeineConsumptionType.EnergyDrink)
        };

        foreach (var consumption in testConsumptions)
        {
            await _database.InsertConsumptionAsync(consumption);
        }
    }

    private static CaffeineConsumption CreateTestConsumption(
        string name,
        int caffeineMg,
        DateTimeOffset consumedAt,
        CaffeineConsumptionType type) => new()
    {
        Name = name,
        CaffeineMg = caffeineMg,
        ConsumedAt = consumedAt.ToUniversalTime(),
        Type = type
    };

    private static DateTimeOffset GetTodayTimestamp(DateTimeOffset now, TimeSpan age)
    {
        var localNow = now.ToLocalTime();
        var startOfToday = new DateTimeOffset(
            localNow.Date,
            TimeZoneInfo.Local.GetUtcOffset(localNow.Date));
        return localNow - age >= startOfToday
            ? localNow - age
            : startOfToday;
    }
#endif

    private static AppSettings CreateDefaultSettings() => new()
    {
        Id = DatabaseConstants.SettingsId
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
}
