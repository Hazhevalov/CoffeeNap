using CoffeeNap.Services;

namespace CoffeeNap.Platforms.Android;

public sealed class AndroidApplicationLifecycleService : IApplicationLifecycleService
{
    public Task RestartApplicationAsync()
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        var component = activity?.ComponentName;
        if (activity is null || component is null)
        {
            throw new InvalidOperationException("The current Android activity is unavailable.");
        }

        // Recreating only the Activity leaves MAUI pages tied to the disposed
        // MauiContext of the old Activity. Submit creation of a new root task to
        // ActivityManager, then terminate this process before its main looper can
        // construct the replacement Activity with the old MAUI application.
        var restartIntent = global::Android.Content.Intent.MakeRestartActivityTask(component);
        activity.StartActivity(restartIntent);
        global::Android.OS.Process.KillProcess(global::Android.OS.Process.MyPid());
        return Task.CompletedTask;
    }

    public void CloseApplication()
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        activity?.FinishAndRemoveTask();
    }
}
