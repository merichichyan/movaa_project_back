namespace movaa_project_back.Domain.Entities;

public class Specialist
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public Guid? SalonId { get; private set; }
    public string? SalonName { get; private set; }
    public double Rating { get; private set; } = 5.0;
    public int ReviewCount { get; private set; } = 0;
    public string? AvatarUrl { get; private set; }
    public bool IsBlocked { get; private set; } = false;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected Specialist() { }

    public Specialist(string name, string category, string phone, string? email = null, Guid? salonId = null, string? salonName = null, string? avatarUrl = null, double rating = 5.0, int reviewCount = 0)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
        Category = category.Trim();
        Phone = phone.Trim();
        Email = email?.Trim();
        SalonId = salonId;
        SalonName = salonName?.Trim();
        AvatarUrl = avatarUrl?.Trim();
        Rating = rating;
        ReviewCount = reviewCount;
        IsBlocked = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string category, string phone, string? email, Guid? salonId, string? salonName, string? avatarUrl)
    {
        Name = name.Trim();
        Category = category.Trim();
        Phone = phone.Trim();
        Email = email?.Trim();
        SalonId = salonId;
        SalonName = salonName?.Trim();
        AvatarUrl = avatarUrl?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetBlocked(bool isBlocked)
    {
        IsBlocked = isBlocked;
        UpdatedAt = DateTime.UtcNow;
    }
}
