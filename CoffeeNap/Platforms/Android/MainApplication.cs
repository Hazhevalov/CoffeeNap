using Android.App;
using Android.Runtime;

namespace CoffeeNap
{
    /// <summary>
    /// Android Application object created by the operating system before the activity.
    /// Delegates application setup to the shared MauiProgram configuration.
    /// </summary>
    [Application]
    public class MainApplication : MauiApplication
    {
        /// <summary>Passes the native JNI handle to the MAUI base class.</summary>
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        /// <summary>Creates the shared MAUI application instance.</summary>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
