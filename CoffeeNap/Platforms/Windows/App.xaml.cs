using Microsoft.UI.Xaml;

namespace CoffeeNap.WinUI
{
    /// <summary>
    /// Native Windows/WinUI entry point, distinct from CoffeeNap.App.
    /// Creates the MAUI host that launches the shared application.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Creates the singleton WinUI application, equivalent to Main or WinMain.
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>Delegates application construction to the shared MauiProgram configuration.</summary>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

}
