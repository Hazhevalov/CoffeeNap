namespace CoffeeNap.Services;

public sealed class DefaultApplicationLifecycleService : IApplicationLifecycleService
{
    public void CloseApplication()
    {
        var application = Application.Current;
        var window = application?.Windows.FirstOrDefault();
        if (application is not null && window is not null)
        {
            application.CloseWindow(window);
        }
    }
}
