namespace movaa_project_back.Application.DTOs.Auth;

public record SpecialistActivationRequestDto(
    string Phone,
    string Email,
    string Password
);
