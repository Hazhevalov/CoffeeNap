namespace CoffeeNap.Services;

public interface IDialogService
{
    Task ShowErrorAsync(string title, string message, string cancel);
}
