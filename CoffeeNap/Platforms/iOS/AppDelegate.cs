using Foundation;

namespace CoffeeNap
{
    /// <summary>
    /// iOS lifecycle delegate exposed to the Objective-C runtime by Register.
    /// Delegates application creation to MauiProgram.
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        /// <summary>Creates the shared MAUI application instance.</summary>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
