using System.Diagnostics;

namespace CoffeeNap.Helpers;

/// <summary>Нулевая в Release DEBUG-диагностика construction/navigation/layout.</summary>
internal static class PerformanceTrace
{
    public static long Start() => Stopwatch.GetTimestamp();

    [Conditional("DEBUG")]
    public static void Elapsed(string operation, long startedAt)
    {
        var message = $"{operation}: " +
                      $"{Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds:F1} ms";
#if ANDROID
        Android.Util.Log.Warn("CNPerf", message);
#else
        Debug.WriteLine($"[CNPerf] {message}");
#endif
    }

    [Conditional("DEBUG")]
    public static void TrackFirstLayout(VisualElement element, string pageName, long startedAt)
    {
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            if (element.Width <= 0 || element.Height <= 0)
            {
                return;
            }

            element.SizeChanged -= handler;
            Elapsed($"{pageName}.first-layout", startedAt);
        };
        element.SizeChanged += handler;
    }
}
