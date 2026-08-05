namespace movaa_project_back.Application.DTOs.Auth;

public record LoginRequestDto(
    string? Phone,
    string? PhoneNumber,
    string Password
);

public record AdminLoginRequestDto(
    string Username,
    string Password
);

public record UserRegisterRequestDto(
    string Phone,
    string Password,
    string? FullName,
    string? Email,
    string? Gender,
    DateTime? Birthday,
    string? DeviceId
);

public record SelectRoleRequestDto(
    Guid UserId,
    string Role
);

public record AuthResponseDto(
    string Token,
    Guid Id,
    string Phone,
    string Email,
    string FullName,
    string Role,
    bool IsOnboardingCompleted
);

public record UserChangePasswordRequestDto(
    string? Phone,
    string? CurrentPassword,
    string NewPassword
);
