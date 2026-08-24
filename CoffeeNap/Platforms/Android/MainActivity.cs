using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace CoffeeNap
{
    /// <summary>
    /// Единственная Android Activity и точка входа пользовательского интерфейса.
    /// Атрибут назначает splash-тему, делает Activity стартовой и сообщает, какие
    /// изменения конфигурации MAUI обработает без полного пересоздания Activity.
    /// </summary>
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, WindowSoftInputMode = SoftInput.AdjustResize, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}
