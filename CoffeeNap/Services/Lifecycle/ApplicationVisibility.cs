namespace CoffeeNap.Services;

/// <summary>Window lifecycle state remains available even while a tab host is unloaded.</summary>
public sealed class ApplicationVisibility
{
    public bool IsActive { get; private set; } = true;
    public event EventHandler? Changed;

    public void SetActive(bool active)
    {
        if (IsActive == active) return;
        IsActive = active;
        if (active) LocalCalendarTime.RefreshZoneKey();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
