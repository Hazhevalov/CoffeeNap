using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

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
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Own insets once, above Shell's changing fragment/page hierarchy.
            // Otherwise a newly attached page can render one frame before its
            // SafeAreaEdges padding arrives from Android.
            WindowCompat.SetDecorFitsSystemWindows(Window!, false);
            var content = FindViewById<Android.Views.View>(Android.Resource.Id.Content)!;
            ViewCompat.SetOnApplyWindowInsetsListener(content, new AppInsetsListener());
            ViewCompat.RequestApplyInsets(content);
        }

        private sealed class AppInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
        {
            public WindowInsetsCompat? OnApplyWindowInsets(Android.Views.View? view, WindowInsetsCompat? insets)
            {
                var safeArea = insets?.GetInsets(
                    WindowInsetsCompat.Type.SystemBars() |
                    WindowInsetsCompat.Type.DisplayCutout() |
                    WindowInsetsCompat.Type.Ime());

                if (view is null || safeArea is null)
                {
                    return insets;
                }

                if (view.PaddingLeft != safeArea.Left || view.PaddingTop != safeArea.Top ||
                    view.PaddingRight != safeArea.Right || view.PaddingBottom != safeArea.Bottom)
                {
                    view.SetPadding(safeArea.Left, safeArea.Top, safeArea.Right, safeArea.Bottom);
                }

                // Child pages must not apply the same system/keyboard padding again.
                return WindowInsetsCompat.Consumed;
            }
        }
    }
}
