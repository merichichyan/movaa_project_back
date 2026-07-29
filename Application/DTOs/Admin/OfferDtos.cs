namespace movaa_project_back.Application.DTOs.Admin;

public record CreateOfferDto(
    string Title,
    string? TitleHy,
    string? TitleEn,
    string? TitleRu,
    string? Subtitle,
    string? SubtitleHy,
    string? SubtitleEn,
    string? SubtitleRu,
    string? BadgeText,
    string? BadgeTextHy,
    string? BadgeTextEn,
    string? BadgeTextRu,
    double? DiscountPercent,
    Guid? SalonId,
    string? SalonName,
    Guid? SpecialistId,
    string? SpecialistName,
    string? ImageUrl,
    bool IsActive = true
);

public record UpdateOfferDto(
    string Title,
    string? TitleHy,
    string? TitleEn,
    string? TitleRu,
    string? Subtitle,
    string? SubtitleHy,
    string? SubtitleEn,
    string? SubtitleRu,
    string? BadgeText,
    string? BadgeTextHy,
    string? BadgeTextEn,
    string? BadgeTextRu,
    double? DiscountPercent,
    Guid? SalonId,
    string? SalonName,
    Guid? SpecialistId,
    string? SpecialistName,
    string? ImageUrl,
    bool IsActive = true
);
