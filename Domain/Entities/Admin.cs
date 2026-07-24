namespace movaa_project_back.Domain.Entities;

public class Admin
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Role { get; private set; } = "admin";
    public string Email { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected Admin() { }

    public Admin(string username, string passwordHash, string fullName, string? email = null)
    {
        Id = Guid.NewGuid();
        Username = username.Trim();
        PasswordHash = passwordHash;
        FullName = string.IsNullOrWhiteSpace(fullName) ? username.Trim() : fullName.Trim();
        Role = "admin";
        Email = string.IsNullOrWhiteSpace(email) ? $"{username.Trim()}@admin.movaa.com" : email.ToLowerInvariant().Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdatePasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }
}
