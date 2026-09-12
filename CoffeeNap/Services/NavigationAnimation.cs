namespace CoffeeNap.Services;

using Microsoft.Maui.Animations;

/// <summary>Shared platform policy for optional navigation motion.</summary>
internal static class NavigationAnimation
{
    internal static bool IsEnabled
    {
        get
        {
            var services = Application.Current?
                .Windows
                .FirstOrDefault()?
                .Page?
                .Handler?
                .MauiContext?
                .Services;
            if (services?.GetService(typeof(IAnimationManager)) is IAnimationManager manager &&
                !manager.Ticker.SystemEnabled)
            {
                return false;
            }

#if ANDROID
            return OperatingSystem.IsAndroidVersionAtLeast(26)
                ? Android.Animation.ValueAnimator.AreAnimatorsEnabled()
                : Microsoft.Maui.Devices.Battery.Default.EnergySaverStatus is not
                    Microsoft.Maui.Devices.EnergySaverStatus.On;
#elif IOS || MACCATALYST
            return !UIKit.UIAccessibility.IsReduceMotionEnabled;
#elif WINDOWS
            return new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
#else
            return true;
#endif
        }
    }
}
