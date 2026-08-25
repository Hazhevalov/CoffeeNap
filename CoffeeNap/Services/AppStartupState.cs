using CoffeeNap.Models;

namespace CoffeeNap.Services;

/// <summary>Минимальные данные, уже загруженные до создания AppShell.</summary>
public sealed class AppStartupState
{
    public UserProfile UserProfile { get; set; } = new();
}
