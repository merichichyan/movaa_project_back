using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using movaa_project_back.Application.DTOs.Auth;
using movaa_project_back.Application.Services;

namespace movaa_project_back.Presentation.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/api/auth").WithTags("Auth");

        authGroup.MapPost("/register", async ([FromBody] UserRegisterRequestDto request, IAuthService authService, CancellationToken ct) =>
        {
            try
            {
                var result = await authService.RegisterUserAsync(request, ct);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        })
        .WithSummary("Register a new user")
        .WithDescription("Registers a new user and returns JWT authentication details.");

        authGroup.MapPost("/register/user", async ([FromBody] UserRegisterRequestDto request, IAuthService authService, CancellationToken ct) =>
        {
            try
            {
                var result = await authService.RegisterUserAsync(request, ct);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        })
        .WithSummary("Register a client user")
        .WithDescription("Registers a client user with profile information and returns JWT authentication details.");

        authGroup.MapPost("/login", async ([FromBody] LoginRequestDto request, IAuthService authService, CancellationToken ct) =>
        {
            try
            {
                var result = await authService.LoginAsync(request, ct);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Unauthorized();
            }
        })
        .WithSummary("Log in user")
        .WithDescription("Authenticates user with phone number and password, returning JWT token.");

        authGroup.MapPost("/select-role", async ([FromBody] SelectRoleRequestDto request, IAuthService authService, CancellationToken ct) =>
        {
            try
            {
                await authService.SelectRoleAsync(request, ct);
                return Results.Ok(new { message = "Role updated successfully.", role = request.Role });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithSummary("Select user role")
        .WithDescription("Updates the role of a user.");

        var usersGroup = app.MapGroup("/api/users").WithTags("Users");
        usersGroup.MapPatch("/onboarding/complete", [Authorize] async (ClaimsPrincipal principal, [FromQuery] Guid? userId, IAuthService authService, CancellationToken ct) =>
        {
            Guid targetUserId;
            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                targetUserId = userId.Value;
            }
            else
            {
                var nameIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(nameIdClaim) || !Guid.TryParse(nameIdClaim, out targetUserId))
                {
                    return Results.Unauthorized();
                }
            }

            try
            {
                await authService.CompleteOnboardingAsync(targetUserId, ct);
                return Results.Ok(new { message = "Onboarding completed successfully.", isOnboardingCompleted = true });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithSummary("Complete onboarding")
        .WithDescription("Marks onboarding status as completed for authenticated user.");

        return app;
    }
}
