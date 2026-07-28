namespace movaa_project_back.Domain.Entities;

public class Specialist
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? JobTitle { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public Guid? SalonId { get; private set; }
    public string? SalonName { get; private set; }
    public double Rating { get; private set; } = 5.0;
    public int ReviewCount { get; private set; } = 0;
    public string? AvatarUrl { get; private set; }
    public string? Bio { get; private set; }
    public int ExperienceYears { get; private set; } = 0;
    public string? WorkingHours { get; private set; }
    public double CommissionRate { get; private set; } = 0.0;
    public bool IsBlocked { get; private set; } = false;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected Specialist() { }

    public Specialist(
        string name,
        string category,
        string phone,
        string? jobTitle = null,
        string? email = null,
        Guid? salonId = null,
        string? salonName = null,
        string? avatarUrl = null,
        string? bio = null,
        int experienceYears = 0,
        string? workingHours = null,
        double commissionRate = 0.0,
        double rating = 5.0,
        int reviewCount = 0)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
        Category = category.Trim();
        Phone = phone.Trim();
        JobTitle = jobTitle?.Trim();
        Email = email?.Trim();
        SalonId = salonId;
        SalonName = salonName?.Trim();
        AvatarUrl = avatarUrl?.Trim();
        Bio = bio?.Trim();
        ExperienceYears = experienceYears;
        WorkingHours = workingHours?.Trim();
        CommissionRate = commissionRate;
        Rating = rating;
        ReviewCount = reviewCount;
        IsBlocked = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name,
        string category,
        string phone,
        string? jobTitle,
        string? email,
        Guid? salonId,
        string? salonName,
        string? avatarUrl,
        string? bio,
        int experienceYears,
        string? workingHours,
        double commissionRate)
    {
        Name = name.Trim();
        Category = category.Trim();
        Phone = phone.Trim();
        JobTitle = jobTitle?.Trim();
        Email = email?.Trim();
        SalonId = salonId;
        SalonName = salonName?.Trim();
        AvatarUrl = avatarUrl?.Trim();
        Bio = bio?.Trim();
        ExperienceYears = experienceYears;
        WorkingHours = workingHours?.Trim();
        CommissionRate = commissionRate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetBlocked(bool isBlocked)
    {
        IsBlocked = isBlocked;
        UpdatedAt = DateTime.UtcNow;
    }
}
