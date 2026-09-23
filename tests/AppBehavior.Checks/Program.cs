using System.Diagnostics;
using CoffeeNap.Data;
using CoffeeNap.Models;
using CoffeeNap.Services;
using Microsoft.Extensions.Logging.Abstractions;
using SQLite;
using CoffeeNap.ViewModels;

if (args is ["--seed", var output, var countText] && int.TryParse(countText, out var count) && count is >= 0 and <= 100_000)
{
    if (File.Exists(output)) throw new InvalidOperationException("Seed output must be a new file.");
    using var seed = new SQLiteConnection(Path.GetFullPath(output));
    seed.CreateTable<CaffeineConsumption>();
    seed.CreateTable<AppSettings>();
    seed.CreateTable<UserProfile>();
    seed.CreateTable<LastConsumptionRecipe>();
    seed.Insert(new AppSettings { Id = DatabaseConstants.SettingsId, LanguageCode = "en" });
    seed.Insert(new UserProfile { Id = DatabaseConstants.UserProfileId, UserName = "Audit", OnboardingCompleted = true });
    var timestamp = DateTimeOffset.UtcNow;
    seed.RunInTransaction(() =>
    {
        for (var i = 0; i < count; i++)
            seed.Insert(new CaffeineConsumption { Name = "Black tea", NameKey = "BlackTeaDrink",
                Type = CaffeineConsumptionType.Tea, CaffeineMg = 40, ConsumedAt = timestamp.AddHours(-(i / 4)) });
        seed.Execute($"PRAGMA user_version = {DatabaseConstants.Version}");
    });
    Console.WriteLine($"Created {count} synthetic records in {Path.GetFullPath(output)}");
    return;
}

var checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new Exception(message);
}

var directory = Path.Combine(Path.GetTempPath(), "CoffeeNap-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
FileSystem.AppDataDirectory = directory;
var path = Path.Combine(directory, DatabaseConstants.FileName);
var now = DateTimeOffset.Now;
try
{
    using (var legacy = new SQLiteConnection(path))
    {
        legacy.CreateTable<LegacyConsumption>();
        legacy.Insert(new LegacyConsumption { Name = "Чёрный чай", Type = CaffeineConsumptionType.Tea, CaffeineMg = 40, ConsumedAt = now });
        legacy.Insert(new LegacyConsumption { Name = "Black tea", Type = CaffeineConsumptionType.Tea, CaffeineMg = 40, ConsumedAt = now });
        legacy.Insert(new LegacyConsumption { Name = "Custom blend", Type = CaffeineConsumptionType.Coffee, CaffeineMg = 80, ConsumedAt = now });
        legacy.Execute("PRAGMA user_version = 3");
    }

    var database = new AppDatabase();
    var data = new AppDataService(database, NullLogger<AppDataService>.Instance);
    await data.InitializeAsync();
    var migrated = await data.GetConsumptionsAsync();
    Check(migrated.Count == 3, "Migration must preserve all rows");
    Check(migrated.Single(row => row.Name == "Black tea").NameKey == "BlackTeaDrink", "English legacy name migration");
    Check(migrated.Single(row => row.Name == "Чёрный чай").NameKey == "BlackTeaDrink", "Russian legacy name migration");
    Check(string.IsNullOrEmpty(migrated.Single(row => row.Name == "Custom blend").NameKey), "Unknown names remain readable");

    var recipe = new LastConsumptionRecipe { DrinkType = CaffeineConsumptionType.Tea, TeaType = TeaType.Black, TeaAmountGrams = 2 };
    var received = 0;
    data.ConsumptionAdded += (_, _) => throw new InvalidOperationException("Broken UI subscriber");
    data.ConsumptionAdded += (_, _) => received++;
    var item = new CaffeineConsumption { Name = "Black tea", Type = CaffeineConsumptionType.Tea, CaffeineMg = 40, ConsumedAt = now };
    await data.AddConsumptionAndSaveRecipeAsync(item, recipe);
    Check(received == 1 && item.Id > 0, "Committed save survives subscriber failure and notifies remaining subscribers");
    Check((await data.GetConsumptionsAsync()).Count == 4, "Exactly one saved row");
    Check(item.NameKey == "BlackTeaDrink", "New records use a language-independent key");
    var deleteReceived = false;
    data.ConsumptionDeleted += (_, _) => throw new InvalidOperationException("Broken delete subscriber");
    data.ConsumptionDeleted += (_, _) => deleteReceived = true;
    await data.DeleteConsumptionAsync(item.Id);
    Check(deleteReceived, "Committed deletion notifies remaining subscribers");

    foreach (var amount in new[] { double.NaN, double.PositiveInfinity, 0, -1, 50.1, 1e8 })
    {
        var invalid = new LastConsumptionRecipe { DrinkType = CaffeineConsumptionType.Tea, TeaType = TeaType.Black, TeaAmountGrams = amount };
        Check(!ConsumptionRecipeValidator.IsValid(invalid), "Reject invalid tea amount");
        var rejected = false;
        try { await data.AddConsumptionAndSaveRecipeAsync(item, invalid); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "Persistence rejects invalid recipe before committing");
    }

    var huge = new[] {
        new CaffeineConsumption { ConsumedAt = now, CaffeineMg = 2_000_000_000 },
        new CaffeineConsumption { ConsumedAt = now, CaffeineMg = 2_000_000_000 }
    };
    Check(CaffeineStatisticsCalculator.CalculateDailyCaffeine(huge, now) == 4_000_000_000d, "Legacy daily totals must not overflow int");

    // Seed realistic history directly, avoiding 10,000 UI event notifications.
    using (var seed = new SQLiteConnection(path))
    {
        seed.RunInTransaction(() =>
        {
            for (var i = 0; i < 10_000; i++)
                seed.Insert(new CaffeineConsumption { Name = "Black tea", NameKey = "BlackTeaDrink", Type = CaffeineConsumptionType.Tea,
                    CaffeineMg = 40, ConsumedAt = now.AddHours(-(i / 4)) });
        });
    }
    var seen = new HashSet<int>();
    ConsumptionCursor? cursor = null;
    while (true)
    {
        var page = await data.GetConsumptionsPageAsync(cursor, 40);
        Check(page.Count <= 40, "Bounded page size");
        if (page.Count == 0) break;
        foreach (var row in page) Check(seen.Add(row.Id), "Keyset paging never duplicates equal timestamps");
        cursor = new ConsumptionCursor(page[^1].ConsumedAt, page[^1].Id);
    }
    Check(seen.Count == 10_003, "Paging returns all existing rows");
    var first = await data.GetConsumptionsPageAsync(null, 40);
    var anchor = new ConsumptionCursor(first[^1].ConsumedAt, first[^1].Id);
    await data.DeleteConsumptionAsync(first[0].Id);
    await data.AddConsumptionAndSaveRecipeAsync(new CaffeineConsumption { Name = "Black tea", Type = CaffeineConsumptionType.Tea, CaffeineMg = 40, ConsumedAt = now.AddSeconds(1) }, recipe);
    var next = await data.GetConsumptionsPageAsync(anchor, 40);
    Check(!next.Any(row => first.Any(old => old.Id == row.Id)), "Insert/delete before cursor cannot shift pages");

    var watch = Stopwatch.StartNew();
    var overview = await data.GetConsumptionOverviewAsync(now.AddMinutes(1));
    var page40 = await data.GetConsumptionsPageAsync(null, 40);
    Console.WriteLine($"Desktop SQLite, 10,003 rows: summary + first page {watch.Elapsed.TotalMilliseconds:F1} ms; materialized {page40.Count} history rows.");
    var all = await data.GetConsumptionsAsync();
    Check(overview.Distribution.TotalCount == all.Count, "Summary includes unloaded history");
    Check(overview.DailyCaffeineMg == CaffeineStatisticsCalculator.CalculateDailyCaffeine(all, now.AddMinutes(1)), "SQL and in-memory daily totals agree");

    var localization = new LocalizationService(data);
    await localization.InitializeAsync();
    var dialogs = new TestDialogs();
    var main = new MainPageViewModel(data, localization, NullLogger<MainPageViewModel>.Instance,
        new MainHeaderViewModel(), dialogs);
    await main.InitializeAsync();
    Check(main.Consumptions.Count == 40 && main.HasMore, "Main screen initially materializes one page");
    Check(main.TeaSource.TotalCount == all.Count, "Main source counts include unloaded history");
    var timeUpdates = 0;
    foreach (var visibleItem in main.Consumptions)
        visibleItem.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(visibleItem.RelativeTime)) timeUpdates++; };
    main.SetVisibleRange(0, 1);
    Check(timeUpdates == 2, "Only visible history rows refresh their relative time");
    main.SetVisibleRange(0, 1);
    Check(timeUpdates == 2, "Repeated refresh does not publish unchanged strings");
    await main.LoadMoreCommand.ExecuteAsync(null);
    Check(main.Consumptions.Count == 80, "Main screen loads another bounded page");
    var originalCount = main.TeaSource.Count;
    var vmItem = new CaffeineConsumption { Name = "Black tea", Type = CaffeineConsumptionType.Tea, CaffeineMg = 40, ConsumedAt = now };
    await data.AddConsumptionAndSaveRecipeAsync(vmItem, recipe);
    Check(main.TeaSource.Count == originalCount + 1, "Main source count updates incrementally");
    await data.DeleteConsumptionAsync(vmItem.Id);
    Check(main.TeaSource.Count == originalCount, "Main source count decreases after deletion");
    var translated = new ConsumptionItemViewModel(new CaffeineConsumption { Name = "Чёрный чай", NameKey = "BlackTeaDrink", ConsumedAt = now });
    Check(translated.Name == "Black tea", "Persisted keys display in the current language");
    var notifications = 0;
    _ = translated.RelativeTime;
    translated.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(translated.RelativeTime)) notifications++; };
    translated.RefreshRelativeTime();
    Check(notifications == 0, "An unchanged relative time does not notify bindings");

    var calendar = new CalendarStatisticsService(data, NullLogger<CalendarStatisticsService>.Instance);
    var month = await calendar.GetMonthAsync(DateTime.Today);
    Check(ReferenceEquals(month, await calendar.GetMonthAsync(DateTime.Today)), "Unchanged month uses cache");
    await data.AddConsumptionAndSaveRecipeAsync(new CaffeineConsumption { Name = "Black tea", Type = CaffeineConsumptionType.Tea, CaffeineMg = 40, ConsumedAt = now }, recipe);
    var updated = await calendar.GetMonthAsync(DateTime.Today);
    Check(!ReferenceEquals(month, updated), "A committed addition invalidates calendar even after another subscriber throws");
    Check(updated.Days[DateOnly.FromDateTime(DateTime.Today)].TotalCaffeineMg == month.Days[DateOnly.FromDateTime(DateTime.Today)].TotalCaffeineMg + 40, "Calendar includes the new entry");

    var calendarVm = new CalendarPageViewModel(calendar, localization,
        NullLogger<CalendarPageViewModel>.Instance, new MainHeaderViewModel());
    var originalTitle = calendarVm.MonthTitle;
    MainThread.HoldActions = true;
    var previousTask = calendarVm.ShowPreviousMonthCommand.ExecuteAsync(null);
    await WaitForUiCallbacksAsync(1);
    var currentTask = calendarVm.ShowNextMonthCommand.ExecuteAsync(null);
    await WaitForUiCallbacksAsync(2);
    MainThread.RunNext();
    Check(calendarVm.MonthTitle == originalTitle, "A queued stale month cannot publish after a newer selection");
    MainThread.HoldActions = false;
    while (!MainThread.Pending.IsEmpty) MainThread.RunNext();
    await Task.WhenAll(previousTask, currentTask);
    Check(calendarVm.MonthTitle == originalTitle, "Latest requested month wins");

    await data.AddConsumptionAndSaveRecipeAsync(new CaffeineConsumption { Name = "Black tea", Type = CaffeineConsumptionType.Tea,
        CaffeineMg = 40, ConsumedAt = new DateTimeOffset(1999, 1, 15, 23, 0, 0, TimeSpan.Zero) }, recipe);
    var clock = new MutableZoneClock();
    var zonedCalendar = new CalendarStatisticsService(data, NullLogger<CalendarStatisticsService>.Instance, clock);
    var east = await zonedCalendar.GetMonthAsync(new DateTime(1999, 1, 1));
    Check(east.Days.ContainsKey(new DateOnly(1999, 1, 16)), "UTC record groups by local day");
    clock.Zone = TimeZoneInfo.CreateCustomTimeZone("West", TimeSpan.FromHours(-3), "West", "West");
    var west = await zonedCalendar.GetMonthAsync(new DateTime(1999, 1, 1));
    Check(west.Days.ContainsKey(new DateOnly(1999, 1, 15)) && !west.Days.ContainsKey(new DateOnly(1999, 1, 16)), "Changing timezone invalidates cached day grouping");

    using (var broken = new SQLiteConnection(path)) broken.DropTable<CaffeineConsumption>();
    await main.DeleteConsumptionCommand.ExecuteAsync(main.Consumptions[0]);
    Check(dialogs.Errors == 1, "Failed deletion is shown to the user");
    var retryVm = new MainPageViewModel(data, localization, NullLogger<MainPageViewModel>.Instance,
        new MainHeaderViewModel(), dialogs);
    await retryVm.InitializeAsync();
    Check(retryVm.HasLoadError && !retryVm.IsInitialized, "Read failure is distinct from an empty history");
    using (var repaired = new SQLiteConnection(path)) repaired.CreateTable<CaffeineConsumption>();
    await retryVm.RetryCommand.ExecuteAsync(null);
    Check(!retryVm.HasLoadError && retryVm.IsInitialized, "Retry recovers after the read failure is resolved");
    var resetReceived = false;
    data.UserDataDeleted += (_, _) => throw new InvalidOperationException("Broken reset subscriber");
    data.UserDataDeleted += (_, _) => resetReceived = true;
    await data.DeleteAllUserDataAsync();
    Check(resetReceived && (await data.GetConsumptionsPageAsync(null, 40)).Count == 0, "Reset commits and survives subscriber failure");
    Check((await calendar.GetMonthAsync(DateTime.Today)).Days.Count == 0, "Reset clears calendar cache");
    Console.WriteLine($"PASS: {checks} persistence, migration, pagination, validation and calendar checks.");
}
finally
{
    SQLiteAsyncConnection.ResetPool();
    if (!Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) ||
        !Path.GetFileName(directory).StartsWith("CoffeeNap-checks-", StringComparison.Ordinal))
        throw new InvalidOperationException("Unexpected test cleanup path.");
    Directory.Delete(directory, recursive: true);
}

static async Task WaitForUiCallbacksAsync(int count)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    while (MainThread.Pending.Count < count) await Task.Delay(10, timeout.Token);
}

public sealed class TestDialogs : IDialogService
{
    public int Errors { get; private set; }
    public Task ShowErrorAsync(string title, string message, string cancel)
    {
        Errors++;
        return Task.CompletedTask;
    }
}

public sealed class MutableZoneClock : TimeProvider
{
    public TimeZoneInfo Zone { get; set; } = TimeZoneInfo.CreateCustomTimeZone("East", TimeSpan.FromHours(3), "East", "East");
    public override TimeZoneInfo LocalTimeZone => Zone;
}

[Table("CaffeineConsumptions")]
public sealed class LegacyConsumption
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset ConsumedAt { get; set; }
    public int CaffeineMg { get; set; }
    public CaffeineConsumptionType Type { get; set; }
}
