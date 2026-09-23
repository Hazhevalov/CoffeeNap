using CoffeeNap.ViewModels;

namespace CoffeeNap.Views;

/// <summary>Lifecycle contract for content hosted by the animated tab surface.</summary>
internal interface ITabContent
{
    NavigationTab Tab { get; }

    Task ActivateAsync();

    void Deactivate();

    void Release();
}
