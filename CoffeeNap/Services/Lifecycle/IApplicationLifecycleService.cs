namespace CoffeeNap.Services;

public interface IApplicationLifecycleService
{
    // Restarts the application using platform-specific behavior.
    Task RestartApplicationAsync();
    // Closes the application using platform-specific behavior.
    void CloseApplication();
}
