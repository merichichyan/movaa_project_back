namespace movaa_project_back.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Phone { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Role { get; private set; } = "user";
    public string Email { get; private set; } = string.Empty;
    public string? Gender { get; private set; }
    public DateTime? Birthday { get; private set; }
    public string? DeviceId { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string Status { get; private set; } = "Pending";
    public bool IsBlocked { get; private set; }
    public bool IsOnboardingCompleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected User() { }

    public User(string phone, string passwordHash, string fullName, string role = "user", string? email = null, string? gender = null, DateTime? birthday = null, string? deviceId = null)
    {
        Id = Guid.NewGuid();
        Phone = phone.Trim();
        PasswordHash = passwordHash;
        FullName = string.IsNullOrWhiteSpace(fullName) ? phone.Trim() : fullName.Trim();
        Role = string.IsNullOrWhiteSpace(role) ? "user" : role.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? $"{System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", "")}@user.movaa.com" : email.ToLowerInvariant().Trim();
        Gender = gender;
        Birthday = birthday;
        DeviceId = deviceId;
        IsOnboardingCompleted = false;
        Status = role.Equals("user", StringComparison.OrdinalIgnoreCase) ? "Verified" : "Pending";
        CreatedAt = DateTime.UtcNow;
    }

    public void CompleteOnboarding()
    {
        IsOnboardingCompleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRole(string role)
    {
        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(string status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePhone(string phone)
    {
        if (!string.IsNullOrWhiteSpace(phone))
        {
            Phone = phone.Trim();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateProfile(string phone, string fullName, string? email, string? gender, DateTime? birthday)
    {
        Phone = phone.Trim();
        FullName = fullName;
        Email = email?.ToLowerInvariant().Trim();
        Gender = gender;
        Birthday = birthday;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetBlocked(bool isBlocked)
    {
        IsBlocked = isBlocked;
        Status = isBlocked ? "Blocked" : "Active";
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAvatar(string? avatarUrl)
    {
        AvatarUrl = avatarUrl?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
