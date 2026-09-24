namespace CoffeeNap.Services;

public interface IDialogService
{
    // Displays an error message to the user.
    Task ShowErrorAsync(string title, string message, string cancel);
}
