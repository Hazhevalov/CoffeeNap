namespace CoffeeNap.Services;

public static class LocalCalendarTime
{
    // Refresh the framework's cached zone after OS settings change or a resume.
    public static string RefreshZoneKey()
    {
        TimeZoneInfo.ClearCachedData();
        return TimeZoneInfo.Local.ToSerializedString();
    }
}
