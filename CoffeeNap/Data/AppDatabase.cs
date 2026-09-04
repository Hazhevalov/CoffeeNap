using CoffeeNap.Models;
using SQLite;

namespace CoffeeNap.Data;

/// <summary>
/// Единственный владелец SQLite-соединения. Инициализация сериализована и не
/// пересоздаёт существующие таблицы или пользовательские данные.
/// </summary>
public sealed class AppDatabase
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly SQLiteAsyncConnection _connection;
    private bool _isInitialized;

    public AppDatabase()
    {
        DatabasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseConstants.FileName);
        _connection = new SQLiteAsyncConnection(
            DatabasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    public string DatabasePath { get; }

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
            await _connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_CaffeineConsumptions_ConsumedAt " +
                "ON CaffeineConsumptions (ConsumedAt)");

            if (databaseVersion < DatabaseConstants.Version)
            {
                // Новые миграции добавляются сюда последовательно, без DropTable.
                await _connection.ExecuteAsync($"PRAGMA user_version = {DatabaseConstants.Version}");
            }

            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task<UserProfile?> GetUserProfileAsync() =>
        await _connection.FindAsync<UserProfile>(DatabaseConstants.UserProfileId);

    public Task<int> SaveUserProfileAsync(UserProfile profile) =>
        _connection.InsertOrReplaceAsync(profile);

    public async Task<AppSettings?> GetSettingsAsync() =>
        await _connection.FindAsync<AppSettings>(DatabaseConstants.SettingsId);

    public Task<int> SaveSettingsAsync(AppSettings settings) =>
        _connection.InsertOrReplaceAsync(settings);

    public Task<List<CaffeineConsumption>> GetConsumptionsAsync() =>
        _connection.Table<CaffeineConsumption>()
            .OrderByDescending(consumption => consumption.ConsumedAt)
            .ToListAsync();

    public async Task<CaffeineConsumption?> GetConsumptionAsync(int id) =>
        await _connection.FindAsync<CaffeineConsumption>(id);

    public Task<List<CaffeineConsumption>> GetConsumptionsBetweenAsync(
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive) =>
        _connection.Table<CaffeineConsumption>()
            .Where(consumption =>
                consumption.ConsumedAt >= fromInclusive &&
                consumption.ConsumedAt < toExclusive)
            .OrderByDescending(consumption => consumption.ConsumedAt)
            .ToListAsync();

    public Task<int> InsertConsumptionAsync(CaffeineConsumption consumption) =>
        _connection.InsertAsync(consumption);

    public Task<int> UpdateConsumptionAsync(CaffeineConsumption consumption) =>
        _connection.UpdateAsync(consumption);

    public Task<int> DeleteConsumptionAsync(int id) =>
        _connection.DeleteAsync<CaffeineConsumption>(id);

    /// <summary>Atomically removes user rows without changing the database schema.</summary>
    public Task DeleteAllUserDataAsync() =>
        _connection.RunInTransactionAsync(connection =>
        {
            connection.DeleteAll<CaffeineConsumption>();
            connection.DeleteAll<UserProfile>();
            connection.DeleteAll<AppSettings>();
        });
}
