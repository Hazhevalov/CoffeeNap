using Microsoft.UI.Xaml;

namespace CoffeeNap.WinUI
{
    /// <summary>
    /// Нативная точка входа Windows/WinUI. Этот класс не следует путать с общим
    /// CoffeeNap.App: он создаёт MAUI-хост, который затем запускает общий App.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Создаёт singleton-объект WinUI-приложения; это эквивалент Main/WinMain.
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>Передаёт сборку приложения общей конфигурации MauiProgram.</summary>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

}
