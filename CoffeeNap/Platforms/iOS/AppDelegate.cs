using Foundation;

namespace CoffeeNap
{
    /// <summary>
    /// Делегат жизненного цикла iOS. Атрибут Register делает класс доступным
    /// Objective-C runtime, а создание приложения делегируется MauiProgram.
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        /// <summary>Создаёт общий экземпляр MAUI-приложения.</summary>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
