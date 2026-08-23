using Microsoft.Extensions.Logging;

namespace CoffeeNap
{
    /// <summary>
    /// Общая точка сборки MAUI-приложения для всех платформ. Android, iOS,
    /// Mac Catalyst и Windows вызывают один и тот же метод <see cref="CreateMauiApp"/>.
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>Регистрирует App, шрифты, логирование и будущие DI-сервисы.</summary>
        public static MauiApp CreateMauiApp()
        {
            // Здесь собираются общие зависимости и ресурсы приложения до запуска.
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    // Один variable-font зарегистрирован под тремя псевдонимами.
                    // Inter используется текущей разметкой; OpenSans оставлены для
                    // совместимости со стандартными стилями шаблона MAUI.
                    fonts.AddFont("InterVariableFont.ttf", "Inter");
                    fonts.AddFont("InterVariableFont.ttf", "OpenSansRegular");
                    fonts.AddFont("InterVariableFont.ttf", "OpenSansSemibold");
                });

#if DEBUG
			// Debug-провайдер пишет диагностические сообщения в окно Output IDE.
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
