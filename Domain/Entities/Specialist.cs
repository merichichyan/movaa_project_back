namespace movaa_project_back.Domain.Entities;

public class Specialist
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NameHy { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameRu { get; private set; } = string.Empty;

    public string? JobTitle { get; private set; }
    public string? JobTitleHy { get; private set; }
    public string? JobTitleEn { get; private set; }
    public string? JobTitleRu { get; private set; }

    public string Category { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public Guid? SalonId { get; private set; }
    public string? SalonName { get; private set; }
    public double Rating { get; private set; } = 5.0;
    public int ReviewCount { get; private set; } = 0;
    public string? AvatarUrl { get; private set; }

    public string? Bio { get; private set; }
    public string? BioHy { get; private set; }
    public string? BioEn { get; private set; }
    public string? BioRu { get; private set; }

    public int ExperienceYears { get; private set; } = 0;
    public string? WorkingHours { get; private set; }
    public double CommissionRate { get; private set; } = 0.0;
    public string ServicesJson { get; private set; } = "[]";
    public bool IsBlocked { get; private set; } = false;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected Specialist() { }

    public Specialist(
        string name,
        string category,
        string phone,
        string? nameHy = null,
        string? nameEn = null,
        string? nameRu = null,
        string? jobTitle = null,
        string? jobTitleHy = null,
        string? jobTitleEn = null,
        string? jobTitleRu = null,
        string? email = null,
        Guid? salonId = null,
        string? salonName = null,
        string? avatarUrl = null,
        string? bio = null,
        string? bioHy = null,
        string? bioEn = null,
        string? bioRu = null,
        int experienceYears = 0,
        string? workingHours = null,
        double commissionRate = 0.0,
        string? servicesJson = null,
        double rating = 5.0,
        int reviewCount = 0)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
        NameHy = !string.IsNullOrWhiteSpace(nameHy) ? nameHy.Trim() : name.Trim();
        NameEn = !string.IsNullOrWhiteSpace(nameEn) ? nameEn.Trim() : string.Empty;
        NameRu = !string.IsNullOrWhiteSpace(nameRu) ? nameRu.Trim() : string.Empty;

        Category = category.Trim();
        Phone = phone.Trim();

        JobTitle = jobTitle?.Trim();
        JobTitleHy = !string.IsNullOrWhiteSpace(jobTitleHy) ? jobTitleHy.Trim() : (jobTitle?.Trim() ?? string.Empty);
        JobTitleEn = !string.IsNullOrWhiteSpace(jobTitleEn) ? jobTitleEn.Trim() : string.Empty;
        JobTitleRu = !string.IsNullOrWhiteSpace(jobTitleRu) ? jobTitleRu.Trim() : string.Empty;

        Email = email?.Trim();
        SalonId = salonId;
        SalonName = salonName?.Trim();
        AvatarUrl = avatarUrl?.Trim();

        Bio = bio?.Trim();
        BioHy = !string.IsNullOrWhiteSpace(bioHy) ? bioHy.Trim() : (bio?.Trim() ?? string.Empty);
        BioEn = !string.IsNullOrWhiteSpace(bioEn) ? bioEn.Trim() : string.Empty;
        BioRu = !string.IsNullOrWhiteSpace(bioRu) ? bioRu.Trim() : string.Empty;

        ExperienceYears = experienceYears;
        WorkingHours = workingHours?.Trim();
        CommissionRate = commissionRate;
        ServicesJson = !string.IsNullOrWhiteSpace(servicesJson) ? servicesJson.Trim() : "[]";
        Rating = rating;
        ReviewCount = reviewCount;
        IsBlocked = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name,
        string category,
        string phone,
        string? nameHy,
        string? nameEn,
        string? nameRu,
        string? jobTitle,
        string? jobTitleHy,
        string? jobTitleEn,
        string? jobTitleRu,
        string? email,
        Guid? salonId,
        string? salonName,
        string? avatarUrl,
        string? bio,
        string? bioHy,
        string? bioEn,
        string? bioRu,
        int experienceYears,
        string? workingHours,
        double commissionRate,
        string? servicesJson = null)
    {
        Name = name.Trim();
        NameHy = !string.IsNullOrWhiteSpace(nameHy) ? nameHy.Trim() : name.Trim();
        NameEn = !string.IsNullOrWhiteSpace(nameEn) ? nameEn.Trim() : string.Empty;
        NameRu = !string.IsNullOrWhiteSpace(nameRu) ? nameRu.Trim() : string.Empty;

        Category = category.Trim();
        Phone = phone.Trim();

        JobTitle = jobTitle?.Trim();
        JobTitleHy = !string.IsNullOrWhiteSpace(jobTitleHy) ? jobTitleHy.Trim() : (jobTitle?.Trim() ?? string.Empty);
        JobTitleEn = !string.IsNullOrWhiteSpace(jobTitleEn) ? jobTitleEn.Trim() : string.Empty;
        JobTitleRu = !string.IsNullOrWhiteSpace(jobTitleRu) ? jobTitleRu.Trim() : string.Empty;

        Email = email?.Trim();
        SalonId = salonId;
        SalonName = salonName?.Trim();
        AvatarUrl = avatarUrl?.Trim();

        Bio = bio?.Trim();
        BioHy = !string.IsNullOrWhiteSpace(bioHy) ? bioHy.Trim() : (bio?.Trim() ?? string.Empty);
        BioEn = !string.IsNullOrWhiteSpace(bioEn) ? bioEn.Trim() : string.Empty;
        BioRu = !string.IsNullOrWhiteSpace(bioRu) ? bioRu.Trim() : string.Empty;

        ExperienceYears = experienceYears;
        WorkingHours = workingHours?.Trim();
        CommissionRate = commissionRate;
        if (!string.IsNullOrWhiteSpace(servicesJson))
        {
            ServicesJson = servicesJson.Trim();
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetBlocked(bool isBlocked)
    {
        IsBlocked = isBlocked;
        UpdatedAt = DateTime.UtcNow;
    }
}
