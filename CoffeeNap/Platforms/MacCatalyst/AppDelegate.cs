using Foundation;

namespace CoffeeNap
{
    /// <summary>
    /// Mac Catalyst lifecycle delegate using the shared configuration
    /// used by the other platforms.
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        /// <summary>Creates the shared MAUI application instance.</summary>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
