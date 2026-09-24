using CoffeeNap.Models;
using SQLite;

namespace CoffeeNap.Data;

/// <summary>
/// Owns the SQLite connection and serializes initialization
/// without recreating existing tables or user data.
/// </summary>
public sealed class AppDatabase
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly SQLiteAsyncConnection _connection;
    private bool _isInitialized;

    // Initializes the app database.
    public AppDatabase()
    {
        DatabasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseConstants.FileName);
        _connection = new SQLiteAsyncConnection(
            DatabasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    public string DatabasePath { get; }

    // Initializes database tables and migrates older schema data once.
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

            var databaseVersion = await _connection.ExecuteScalarAsync<int>("PRAGMA user_version");
            if (databaseVersion > DatabaseConstants.Version)
            {
                throw new InvalidOperationException(
                    $"Database version {databaseVersion} is newer than supported version {DatabaseConstants.Version}.");
            }

            await _connection.CreateTableAsync<UserProfile>();
            await _connection.CreateTableAsync<AppSettings>();
            await _connection.CreateTableAsync<CaffeineConsumption>();
            await _connection.CreateTableAsync<LastConsumptionRecipe>();

            if (databaseVersion < DatabaseConstants.Version)
            {
                // Keep original text as a fallback for unrecognized legacy/custom names.
                var resources = new System.Resources.ResourceManager(
                    "CoffeeNap.Resources.Localization.AppResources", typeof(AppDatabase).Assembly);
                await _connection.RunInTransactionAsync(connection =>
                {
                    foreach (var key in ConsumptionNameKey.KnownKeys)
                    {
                        foreach (var language in new[] { "ru", "en" })
                        {
                            var name = resources.GetString(key, new System.Globalization.CultureInfo(language));
                            if (!string.IsNullOrEmpty(name))
                                connection.Execute("UPDATE CaffeineConsumptions SET NameKey = ? " +
                                    "WHERE (NameKey IS NULL OR NameKey = '') AND Name = ?", key, name);
                        }
                    }
                });
                await _connection.ExecuteAsync($"PRAGMA user_version = {DatabaseConstants.Version}");
            }

            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    // Loads the saved user profile.
    public async Task<UserProfile?> GetUserProfileAsync() =>
        await _connection.FindAsync<UserProfile>(DatabaseConstants.UserProfileId);

    // Saves the user profile.
    public Task<int> SaveUserProfileAsync(UserProfile profile) =>
        _connection.InsertOrReplaceAsync(profile);

    // Loads application settings.
    public async Task<AppSettings?> GetSettingsAsync() =>
        await _connection.FindAsync<AppSettings>(DatabaseConstants.SettingsId);

    // Saves application settings.
    public Task<int> SaveSettingsAsync(AppSettings settings) =>
        _connection.InsertOrReplaceAsync(settings);

    // Loads the consumption history.
    public Task<List<CaffeineConsumption>> GetConsumptionsAsync() =>
        _connection.Table<CaffeineConsumption>()
            .OrderByDescending(consumption => consumption.ConsumedAt)
            .ToListAsync();

    // Loads a consumption record by its identifier.
    public async Task<CaffeineConsumption?> GetConsumptionAsync(int id) =>
        await _connection.FindAsync<CaffeineConsumption>(id);

    // Loads a page of consumption records using the supplied cursor.
    public Task<List<CaffeineConsumption>> GetConsumptionsPageAsync(ConsumptionCursor? before, int pageSize)
    {
        var query = _connection.Table<CaffeineConsumption>();
        if (before is { } cursor)
        {
            var date = cursor.ConsumedAt;
            var id = cursor.Id;
            query = query.Where(item => item.ConsumedAt < date ||
                (item.ConsumedAt == date && item.Id < id));
        }

        return query.OrderByDescending(item => item.ConsumedAt)
            .ThenByDescending(item => item.Id).Take(pageSize).ToListAsync();
    }

    // Loads daily caffeine totals and consumption counts.
    public async Task<ConsumptionOverview> GetConsumptionOverviewAsync(DateTimeOffset now)
    {
        var date = now.ToLocalTime().Date;
        var start = new DateTimeOffset(date, TimeZoneInfo.Local.GetUtcOffset(date)).ToUniversalTime();
        ConsumptionOverview result = null!;
        await _connection.RunInTransactionAsync(connection =>
        {
            var daily = connection.ExecuteScalar<double>(
                "SELECT COALESCE(SUM(CAST(MAX(0, CaffeineMg) AS REAL)), 0) FROM CaffeineConsumptions " +
                "WHERE ConsumedAt >= ? AND ConsumedAt <= ?", start, now.ToUniversalTime());
            var counts = connection.Query<TypeCount>(
                "SELECT Type, COUNT(*) AS Count FROM CaffeineConsumptions GROUP BY Type");
            result = new ConsumptionOverview(daily, new ConsumptionTypeDistribution(
                counts.FirstOrDefault(item => item.Type == CaffeineConsumptionType.Coffee)?.Count ?? 0,
                counts.FirstOrDefault(item => item.Type == CaffeineConsumptionType.Tea)?.Count ?? 0,
                counts.FirstOrDefault(item => item.Type == CaffeineConsumptionType.EnergyDrink)?.Count ?? 0));
        });
        return result;
    }

    private sealed class TypeCount
    {
        public CaffeineConsumptionType Type { get; set; }
        public int Count { get; set; }
    }

    // Loads consumption records within the specified time range.
    public Task<List<CaffeineConsumption>> GetConsumptionsBetweenAsync(
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive) =>
        _connection.Table<CaffeineConsumption>()
            .Where(consumption =>
                consumption.ConsumedAt >= fromInclusive &&
                consumption.ConsumedAt < toExclusive)
            .OrderByDescending(consumption => consumption.ConsumedAt)
            .ToListAsync();

    // Loads the last saved consumption recipe.
    public async Task<LastConsumptionRecipe?> GetLastConsumptionRecipeAsync() =>
        await _connection.FindAsync<LastConsumptionRecipe>(
            DatabaseConstants.LastConsumptionRecipeId);

    /// <summary>
    /// Saves a consumption and replaces the singleton recipe in one SQLite transaction.
    /// </summary>
    public Task SaveConsumptionAndRecipeAsync(
        CaffeineConsumption consumption,
        LastConsumptionRecipe recipe) =>
        _connection.RunInTransactionAsync(connection =>
        {
            connection.Insert(consumption);
            connection.InsertOrReplace(recipe);
        });

    // Deletes a consumption record by its identifier.
    public Task<int> DeleteConsumptionAsync(int id) =>
        _connection.DeleteAsync<CaffeineConsumption>(id);

    /// <summary>Atomically removes user rows without changing the database schema.</summary>
    public Task DeleteAllUserDataAsync() =>
        _connection.RunInTransactionAsync(connection =>
        {
            connection.DeleteAll<CaffeineConsumption>();
            connection.DeleteAll<LastConsumptionRecipe>();
            connection.DeleteAll<UserProfile>();
            connection.DeleteAll<AppSettings>();
        });
}
