using CoffeeNap.Models;
using SQLite;

namespace CoffeeNap.Data;

/// <summary>
/// Единственный владелец SQLite-соединения. Инициализация сериализована и не
/// пересоздаёт существующие таблицы или пользовательские данные.
/// </summary>
public sealed class AppDatabase
{
    public const int CurrentDatabaseVersion = 1;
    public const string DatabaseFileName = "caffeine_app.db3";

    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private readonly SQLiteAsyncConnection connection;
    private bool isInitialized;

    public AppDatabase()
    {
        DatabasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
        connection = new SQLiteAsyncConnection(
            DatabasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    public string DatabasePath { get; }

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

            var databaseVersion = await connection.ExecuteScalarAsync<int>("PRAGMA user_version");
            if (databaseVersion > CurrentDatabaseVersion)
            {
                throw new InvalidOperationException(
                    $"Database version {databaseVersion} is newer than supported version {CurrentDatabaseVersion}.");
            }

            await connection.CreateTableAsync<UserProfile>();
            await connection.CreateTableAsync<AppSettings>();
            await connection.CreateTableAsync<CaffeineConsumption>();
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_CaffeineConsumptions_ConsumedAt " +
                "ON CaffeineConsumptions (ConsumedAt)");

            if (databaseVersion < CurrentDatabaseVersion)
            {
                // Новые миграции добавляются сюда последовательно, без DropTable.
                await connection.ExecuteAsync($"PRAGMA user_version = {CurrentDatabaseVersion}");
            }

            isInitialized = true;
        }
        finally
        {
            initializationLock.Release();
        }
    }

    public async Task<UserProfile?> GetUserProfileAsync() =>
        await connection.FindAsync<UserProfile>(UserProfile.SingletonId);

    public Task<int> SaveUserProfileAsync(UserProfile profile) =>
        connection.InsertOrReplaceAsync(profile);

    public async Task<AppSettings?> GetSettingsAsync() =>
        await connection.FindAsync<AppSettings>(AppSettings.SingletonId);

    public Task<int> SaveSettingsAsync(AppSettings settings) =>
        connection.InsertOrReplaceAsync(settings);

    public Task<List<CaffeineConsumption>> GetConsumptionsAsync() =>
        connection.Table<CaffeineConsumption>()
            .OrderByDescending(consumption => consumption.ConsumedAt)
            .ToListAsync();

    public Task<List<CaffeineConsumption>> GetConsumptionsBetweenAsync(
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive) =>
        connection.Table<CaffeineConsumption>()
            .Where(consumption =>
                consumption.ConsumedAt >= fromInclusive &&
                consumption.ConsumedAt < toExclusive)
            .OrderByDescending(consumption => consumption.ConsumedAt)
            .ToListAsync();

    public Task<int> InsertConsumptionAsync(CaffeineConsumption consumption) =>
        connection.InsertAsync(consumption);

    public Task<int> UpdateConsumptionAsync(CaffeineConsumption consumption) =>
        connection.UpdateAsync(consumption);

    public Task<int> DeleteConsumptionAsync(int id) =>
        connection.DeleteAsync<CaffeineConsumption>(id);
}
