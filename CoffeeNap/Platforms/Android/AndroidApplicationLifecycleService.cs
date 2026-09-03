using CoffeeNap.Services;

namespace CoffeeNap.Platforms.Android;

public sealed class AndroidApplicationLifecycleService : IApplicationLifecycleService
{
    public void CloseApplication()
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        activity?.FinishAndRemoveTask();
    }
}
