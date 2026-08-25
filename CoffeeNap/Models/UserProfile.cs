using SQLite;

namespace CoffeeNap.Models;

/// <summary>Единственный локальный профиль пользователя приложения.</summary>
[Table("UserProfiles")]
public sealed class UserProfile
{
    public const int SingletonId = 1;

    [PrimaryKey]
    public int Id { get; set; } = SingletonId;

    [MaxLength(15)]
    public string UserName { get; set; } = string.Empty;

    public bool OnboardingCompleted { get; set; }
}
