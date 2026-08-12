namespace movaa_project_back.Application.DTOs.Specialist;

public record CreateSocialLinkDto(
    string? Platform,
    string Url,
    int? DisplayOrder
);

public record UpdateSocialLinkDto(
    string? Platform,
    string Url,
    int? DisplayOrder
);

public record ReorderSocialLinksDto(
    List<Guid> LinkIds
);

public record SocialLinkDto(
    Guid Id,
    Guid SpecialistId,
    string Platform,
    string Url,
    int DisplayOrder,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
