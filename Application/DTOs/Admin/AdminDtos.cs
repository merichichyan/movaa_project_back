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
    string? Email,
    Guid? SalonId,
    string? SalonName,
    string? AvatarUrl
);

public record UpdateSpecialistDto(
    string Name,
    string Category,
    string Phone,
    string? Email,
    Guid? SalonId,
    string? SalonName,
    string? AvatarUrl
);
