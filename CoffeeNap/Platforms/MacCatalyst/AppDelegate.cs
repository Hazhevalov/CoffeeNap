using Foundation;

namespace CoffeeNap
{
    /// <summary>
    /// Делегат жизненного цикла Mac Catalyst. Использует ту же общую конфигурацию,
    /// что и остальные платформы.
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        /// <summary>Создаёт общий экземпляр MAUI-приложения.</summary>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
