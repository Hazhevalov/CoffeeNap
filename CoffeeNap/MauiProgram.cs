using Microsoft.Extensions.Logging;

namespace CoffeeNap
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // Здесь собираются общие зависимости и ресурсы приложения до запуска.
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("InterVariableFont.ttf", "Inter");
                    fonts.AddFont("InterVariableFont.ttf", "OpenSansRegular");
                    fonts.AddFont("InterVariableFont.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
