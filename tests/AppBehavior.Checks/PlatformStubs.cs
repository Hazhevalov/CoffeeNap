// The tests use real SQLite and production services, without starting MAUI.
public static class FileSystem
{
    public static string AppDataDirectory { get; set; } = string.Empty;
}

public sealed class Preferences
{
    public static Preferences Default { get; } = new();
    public T Get<T>(string key, T defaultValue) => defaultValue;
    public void Remove(string key) { }
}

public static class MainThread
{
    public static bool IsMainThread => true;
    public static readonly System.Collections.Concurrent.ConcurrentQueue<(Action Action, TaskCompletionSource Completion)> Pending = new();
    public static bool HoldActions;
    public static Task InvokeOnMainThreadAsync(Action action)
    {
        if (!HoldActions) { action(); return Task.CompletedTask; }
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Pending.Enqueue((action, completion));
        return completion.Task;
    }
    public static Task InvokeOnMainThreadAsync(Func<Task> action) => action();
    public static void RunNext()
    {
        if (!Pending.TryDequeue(out var item)) throw new InvalidOperationException("No queued UI callback");
        item.Action();
        item.Completion.SetResult();
    }
}

public sealed class Color
{
    public static Color FromArgb(string value) => new();
}
public static class Colors
{
    public static Color Black { get; } = new();
    public static Color White { get; } = new();
    public static Color Transparent { get; } = new();
    public static Color Lime { get; } = new();
    public static Color Yellow { get; } = new();
    public static Color Orange { get; } = new();
    public static Color Red { get; } = new();
}
public enum GridUnitType { Star }
public readonly record struct GridLength(double Value, GridUnitType Type = GridUnitType.Star);
public sealed class Application
{
    public static Application? Current => null;
    public Dictionary<string, object> Resources { get; } = [];
}
namespace CoffeeNap.ViewModels
{
    public sealed class MainHeaderViewModel { }
}
