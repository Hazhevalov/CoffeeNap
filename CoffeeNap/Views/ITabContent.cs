using CoffeeNap.ViewModels;

namespace CoffeeNap.Views;

/// <summary>Lifecycle contract for content hosted by the animated tab surface.</summary>
internal interface ITabContent
{
    NavigationTab Tab { get; }

    // Activates tab content when it becomes visible.
    Task ActivateAsync();

    // Suspends tab content while it is hidden.
    void Deactivate();

    // Releases resources owned by the tab content.
    void Release();
}
