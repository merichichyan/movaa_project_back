namespace movaa_project_back.Application.DTOs.Admin;

public record ChangePasswordRequestDto(
    string NewPassword
);

public record AdminLoginRequestDto(
    string Username,
    string Password
);

public record BlockToggleDto(
    bool IsBlocked
);

public record UpdateUserDto(
    string FullName,
    string Phone,
    string? Email
);

public record CreateSalonDto(
    string Name,
    string? NameHy,
    string? NameEn,
    string? NameRu,
    string Address,
    string? AddressHy,
    string? AddressEn,
    string? AddressRu,
    string? Phone,
    string? PhoneNumber,
    string? Category,
    string? WorkingHours,
    string? Email,
    string? Description,
    string? DescriptionHy,
    string? DescriptionEn,
    string? DescriptionRu,
    string? LogoUrl,
    string? OwnerName,
    string? OwnerNameHy,
    string? OwnerNameEn,
    string? OwnerNameRu,
    string? OwnerFullName,
    string? OwnerPhone,
    string? OwnerPhoneNumber,
    string? TaxId,
    List<string>? AdditionalPhones = null,
    string? AdditionalPhonesJson = null
);

public record UpdateSalonDto(
    string Name,
    string? NameHy,
    string? NameEn,
    string? NameRu,
    string Address,
    string? AddressHy,
    string? AddressEn,
    string? AddressRu,
    string? Phone,
    string? PhoneNumber,
    string? Category,
    string? WorkingHours,
    string? Email,
    string? Description,
    string? DescriptionHy,
    string? DescriptionEn,
    string? DescriptionRu,
    string? LogoUrl,
    string? OwnerName,
    string? OwnerNameHy,
    string? OwnerNameEn,
    string? OwnerNameRu,
    string? OwnerFullName,
    string? OwnerPhone,
    string? OwnerPhoneNumber,
    string? TaxId,
    List<string>? AdditionalPhones = null,
    string? AdditionalPhonesJson = null
);

public record CreateSpecialistDto(
    string Name,
    string? NameHy,
    string? NameEn,
    string? NameRu,
    string Category,
    string Phone,
    string? JobTitle,
    string? JobTitleHy,
    string? JobTitleEn,
    string? JobTitleRu,
    string? Email,
    Guid? SalonId,
    string? SalonName,
    string? AvatarUrl,
    string? Bio,
    string? BioHy,
    string? BioEn,
    string? BioRu,
    int? ExperienceYears,
    string? WorkingHours,
    double? CommissionRate,
    string? ServicesJson,
    string? WorkplacesJson,
    List<string>? AdditionalPhones,
    string? AdditionalPhonesJson
);

public record UpdateSpecialistDto(
    string Name,
    string? NameHy,
    string? NameEn,
    string? NameRu,
    string Category,
    string Phone,
    string? JobTitle,
    string? JobTitleHy,
    string? JobTitleEn,
    string? JobTitleRu,
    string? Email,
    Guid? SalonId,
    string? SalonName,
    string? AvatarUrl,
    string? Bio,
    string? BioHy,
    string? BioEn,
    string? BioRu,
    int? ExperienceYears,
    string? WorkingHours,
    double? CommissionRate,
    string? ServicesJson,
    string? WorkplacesJson,
    List<string>? AdditionalPhones,
    string? AdditionalPhonesJson
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

public record RejectPhoneRequestDto(
    string? Note = null,
    string? NoteHy = null,
    string? NoteEn = null,
    string? NoteRu = null
);
