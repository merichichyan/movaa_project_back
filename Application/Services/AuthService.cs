using BCrypt.Net;
using movaa_project_back.Application.DTOs.Auth;
using movaa_project_back.Domain.Entities;
using movaa_project_back.Domain.Interfaces;

namespace movaa_project_back.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthService(IUserRepository userRepository, IJwtTokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponseDto> RegisterUserAsync(UserRegisterRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            throw new ArgumentException("Phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password is required.");
        }

        var existingUser = await _userRepository.GetByPhoneAsync(request.Phone, ct);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this phone number already exists.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User(
            phone: request.Phone,
            passwordHash: passwordHash,
            fullName: request.FullName ?? request.Phone,
            role: "user",
            email: request.Email,
            gender: request.Gender,
            birthday: request.Birthday,
            deviceId: request.DeviceId
        );

        await _userRepository.AddAsync(user, ct);

        var token = _tokenGenerator.GenerateToken(user);

        return new AuthResponseDto(
            Token: token,
            Id: user.Id,
            Phone: user.Phone,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role,
            IsOnboardingCompleted: user.IsOnboardingCompleted
        );
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            throw new ArgumentException("Phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password is required.");
        }

        var user = await _userRepository.GetByPhoneAsync(request.Phone, ct);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid phone number or password.");
        }

        var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Invalid phone number or password.");
        }

        var token = _tokenGenerator.GenerateToken(user);

        return new AuthResponseDto(
            Token: token,
            Id: user.Id,
            Phone: user.Phone,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role,
            IsOnboardingCompleted: user.IsOnboardingCompleted
        );
    }

    public async Task SelectRoleAsync(SelectRoleRequestDto request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        user.UpdateRole(request.Role);
        await _userRepository.UpdateAsync(user, ct);
    }

    public async Task CompleteOnboardingAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        user.CompleteOnboarding();
        await _userRepository.UpdateAsync(user, ct);
    }
}
