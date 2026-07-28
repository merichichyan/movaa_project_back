namespace movaa_project_back.Application.DTOs.Admin;

public record ChangePasswordRequestDto(
    string NewPassword
);

public record BlockToggleDto(
    bool IsBlocked
);

public record CreateSalonDto(
    string Name,
    string Address,
    string? Phone,
    string? PhoneNumber,
    string? Category,
    string? WorkingHours,
    string? Email,
    string? Description,
    string? LogoUrl,
    string? OwnerName,
    string? OwnerFullName,
    string? OwnerPhone,
    string? OwnerPhoneNumber,
    string? TaxId
);

public record UpdateSalonDto(
    string Name,
    string Address,
    string? Phone,
    string? PhoneNumber,
    string? Category,
    string? WorkingHours,
    string? Email,
    string? Description,
    string? LogoUrl,
    string? OwnerName,
    string? OwnerFullName,
    string? OwnerPhone,
    string? OwnerPhoneNumber,
    string? TaxId
);

public record CreateSpecialistDto(
    string Name,
    string Category,
    string Phone,
    string? JobTitle,
    string? Email,
    Guid? SalonId,
    string? SalonName,
    string? AvatarUrl,
    string? Bio,
    int? ExperienceYears,
    string? WorkingHours,
    double? CommissionRate
);

public record UpdateSpecialistDto(
    string Name,
    string Category,
    string Phone,
    string? JobTitle,
    string? Email,
    Guid? SalonId,
    string? SalonName,
    string? AvatarUrl,
    string? Bio,
    int? ExperienceYears,
    string? WorkingHours,
    double? CommissionRate
);

public record CreateCategoryDto(
    string NameHy,
    string NameEn,
    string NameRu,
    string? IconName,
    int? DisplayOrder
);

public record UpdateCategoryDto(
    string NameHy,
    string NameEn,
    string NameRu,
    string? IconName,
    int? DisplayOrder,
    bool IsActive
);
