namespace CoffeeNap.Services;

public sealed class DialogService : IDialogService
{
    public Task ShowErrorAsync(string title, string message, string cancel)
    {
        var page = Shell.Current?.CurrentPage;
        return page is null
            ? Task.CompletedTask
            : page.DisplayAlertAsync(title, message, cancel);
    }
}
