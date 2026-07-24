using movaa_project_back.Application.DTOs.Auth;

namespace movaa_project_back.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterUserAsync(UserRegisterRequestDto request, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task SelectRoleAsync(SelectRoleRequestDto request, CancellationToken ct = default);
    Task CompleteOnboardingAsync(Guid userId, CancellationToken ct = default);
}
