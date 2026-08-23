using Android.App;
using Android.Runtime;

namespace CoffeeNap
{
    /// <summary>
    /// Android-объект Application. Создаётся операционной системой раньше Activity
    /// и передаёт управление общей конфигурации MauiProgram.
    /// </summary>
    [Application]
    public class MainApplication : MauiApplication
    {
        /// <summary>Передаёт нативный JNI-дескриптор базовому классу MAUI.</summary>
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        /// <summary>Создаёт общий экземпляр MAUI-приложения.</summary>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
