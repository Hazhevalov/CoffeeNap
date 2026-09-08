namespace CoffeeNap.Models;

public sealed record PrivacyPolicySection(string Title, string Body)
{
    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
}
