namespace CoffeeNap.Services;

public sealed class DefaultApplicationLifecycleService : IApplicationLifecycleService
{
    // Closes the application so it can be reopened manually.
    public Task RestartApplicationAsync()
    {
        CloseApplication();
        return Task.CompletedTask;
    }

    // Closes the current application.
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
