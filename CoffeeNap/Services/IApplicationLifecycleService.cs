namespace CoffeeNap.Services;

public interface IApplicationLifecycleService
{
    Task RestartApplicationAsync();
    void CloseApplication();
}
