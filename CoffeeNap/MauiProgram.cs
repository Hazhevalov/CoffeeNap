using Microsoft.Extensions.Logging;

namespace CoffeeNap;

public static class MauiProgram
{
    // Builds the MAUI application using the shared configuration.
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("InterVariableFont.ttf", "Inter");
            });

#if ANDROID
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(
            nameof(Controls.BorderlessEntry),
            static (handler, view) =>
            {
                if (view is Controls.BorderlessEntry)
                {
                    handler.PlatformView.Background = null;
                }
            });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddCoffeeNap();

        return builder.Build();
    }
}
