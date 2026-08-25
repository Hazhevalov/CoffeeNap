using SQLite;

namespace CoffeeNap.Models;

/// <summary>Единственный локальный профиль пользователя приложения.</summary>
[Table("UserProfiles")]
public sealed class UserProfile
{
    public const int MaximumUserNameLength = 15;

    [PrimaryKey]
    public int Id { get; set; }

    [MaxLength(MaximumUserNameLength)]
    public string UserName { get; set; } = string.Empty;

    public bool OnboardingCompleted { get; set; }
}
