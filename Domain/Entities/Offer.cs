namespace movaa_project_back.Domain.Entities;

public class Offer
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string TitleHy { get; private set; } = string.Empty;
    public string TitleEn { get; private set; } = string.Empty;
    public string TitleRu { get; private set; } = string.Empty;

    public string Subtitle { get; private set; } = string.Empty;
    public string SubtitleHy { get; private set; } = string.Empty;
    public string SubtitleEn { get; private set; } = string.Empty;
    public string SubtitleRu { get; private set; } = string.Empty;

    public string BadgeText { get; private set; } = "SPECIAL OFFER";
    public string BadgeTextHy { get; private set; } = "ՀԱՏՈՒԿ ԱՌԱՋԱՐԿ";
    public string BadgeTextEn { get; private set; } = "SPECIAL OFFER";
    public string BadgeTextRu { get; private set; } = "СПЕЦИАЛЬНОЕ ПРЕДЛОЖЕНИЕ";

    public double? DiscountPercent { get; private set; }
    public Guid? SalonId { get; private set; }
    public string? SalonName { get; private set; }
    public Guid? SpecialistId { get; private set; }
    public string? SpecialistName { get; private set; }

    public string? ImageUrl { get; private set; }
    public string? ValidUntil { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected Offer() { }

    public Offer(
        string title,
        string? titleHy = null,
        string? titleEn = null,
        string? titleRu = null,
        string? subtitle = null,
        string? subtitleHy = null,
        string? subtitleEn = null,
        string? subtitleRu = null,
        string? badgeText = null,
        string? badgeTextHy = null,
        string? badgeTextEn = null,
        string? badgeTextRu = null,
        double? discountPercent = null,
        Guid? salonId = null,
        string? salonName = null,
        Guid? specialistId = null,
        string? specialistName = null,
        string? imageUrl = null,
        string? validUntil = null,
        bool isActive = true)
    {
        Id = Guid.NewGuid();
        Title = title;
        TitleHy = !string.IsNullOrWhiteSpace(titleHy) ? titleHy : title;
        TitleEn = !string.IsNullOrWhiteSpace(titleEn) ? titleEn : title;
        TitleRu = !string.IsNullOrWhiteSpace(titleRu) ? titleRu : title;

        Subtitle = subtitle ?? string.Empty;
        SubtitleHy = subtitleHy ?? Subtitle;
        SubtitleEn = subtitleEn ?? Subtitle;
        SubtitleRu = subtitleRu ?? Subtitle;

        BadgeText = !string.IsNullOrWhiteSpace(badgeText) ? badgeText : "SPECIAL OFFER";
        BadgeTextHy = !string.IsNullOrWhiteSpace(badgeTextHy) ? badgeTextHy : BadgeText;
        BadgeTextEn = !string.IsNullOrWhiteSpace(badgeTextEn) ? badgeTextEn : BadgeText;
        BadgeTextRu = !string.IsNullOrWhiteSpace(badgeTextRu) ? badgeTextRu : BadgeText;

        DiscountPercent = discountPercent;
        SalonId = salonId;
        SalonName = salonName;
        SpecialistId = specialistId;
        SpecialistName = specialistName;
        ImageUrl = imageUrl;
        ValidUntil = validUntil;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string title,
        string? titleHy,
        string? titleEn,
        string? titleRu,
        string? subtitle,
        string? subtitleHy,
        string? subtitleEn,
        string? subtitleRu,
        string? badgeText,
        string? badgeTextHy,
        string? badgeTextEn,
        string? badgeTextRu,
        double? discountPercent,
        Guid? salonId,
        string? salonName,
        Guid? specialistId,
        string? specialistName,
        string? imageUrl,
        string? validUntil,
        bool isActive)
    {
        Title = title;
        TitleHy = !string.IsNullOrWhiteSpace(titleHy) ? titleHy : title;
        TitleEn = !string.IsNullOrWhiteSpace(titleEn) ? titleEn : title;
        TitleRu = !string.IsNullOrWhiteSpace(titleRu) ? titleRu : title;

        Subtitle = subtitle ?? string.Empty;
        SubtitleHy = subtitleHy ?? Subtitle;
        SubtitleEn = subtitleEn ?? Subtitle;
        SubtitleRu = subtitleRu ?? Subtitle;

        BadgeText = !string.IsNullOrWhiteSpace(badgeText) ? badgeText : "SPECIAL OFFER";
        BadgeTextHy = !string.IsNullOrWhiteSpace(badgeTextHy) ? badgeTextHy : BadgeText;
        BadgeTextEn = !string.IsNullOrWhiteSpace(badgeTextEn) ? badgeTextEn : BadgeText;
        BadgeTextRu = !string.IsNullOrWhiteSpace(badgeTextRu) ? badgeTextRu : BadgeText;

        DiscountPercent = discountPercent;
        SalonId = salonId;
        SalonName = salonName;
        SpecialistId = specialistId;
        SpecialistName = specialistName;
        if (!string.IsNullOrWhiteSpace(imageUrl)) ImageUrl = imageUrl;
        ValidUntil = validUntil;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ToggleActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
