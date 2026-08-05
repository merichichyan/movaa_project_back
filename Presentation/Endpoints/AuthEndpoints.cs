using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movaa_project_back.Application.DTOs.Auth;
using movaa_project_back.Application.Services;
using movaa_project_back.Data;
using movaa_project_back.Domain.Entities;

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

        authGroup.MapPost("/activate-specialist", async ([FromBody] SpecialistActivationRequestDto request, IAuthService authService, CancellationToken ct) =>
        {
            try
            {
                var result = await authService.ActivateSpecialistAccountAsync(request, ct);
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
        .WithSummary("Activate specialist account")
        .WithDescription("Activates a pre-registered specialist account with phone, email, and password.");

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
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .WithSummary("Log in user")
        .WithDescription("Authenticates user with phone number and password, returning JWT token.");

        // Direct alias for clients requesting /api/login
        app.MapPost("/api/login", async ([FromBody] LoginRequestDto request, IAuthService authService, CancellationToken ct) =>
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
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .WithTags("Auth")
        .WithSummary("Log in user (Direct endpoint)");



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

        authGroup.MapDelete("/users/cleanup", async (AppDbContext dbContext, CancellationToken ct) =>
        {
            var count = await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM \"Users\";", ct);
            return Results.Ok(new { message = "All users cleared successfully.", deletedCount = count });
        })
        .WithSummary("Clear all users (Cleanup Endpoint)")
        .WithDescription("Deletes all registered users from the database.");

        authGroup.MapDelete("/users/phone/{phone}", async (string phone, AppDbContext dbContext, CancellationToken ct) =>
        {
            var raw = phone.Trim();
            var count = await dbContext.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"Users\" WHERE \"Phone\" = {0} OR \"Phone\" = {1} OR \"Phone\" = {2};",
                raw, $"+374{raw}", $"+374 {raw}");
            return Results.Ok(new { message = $"Users matching phone '{phone}' deleted successfully.", deletedCount = count });
        })
        .WithSummary("Delete specific user by phone")
        .WithDescription("Deletes a specific user from the database matching the provided phone number.");

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

        authGroup.MapPost("/change-password", async ([FromBody] UserChangePasswordRequestDto request, IAuthService authService, CancellationToken ct) =>
        {
            try
            {
                await authService.ChangeUserPasswordAsync(request, ct);
                return Results.Ok(new { message = "Գաղտնաբառը հաջողությամբ փոխվել է:" });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithSummary("Change user/specialist password")
        .WithDescription("Changes password for user or specialist.");

        authGroup.MapPost("/phone-change-request", async ([FromBody] SpecialistPhoneChangeRequestDto dto, AppDbContext dbContext, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPhone) || string.IsNullOrWhiteSpace(dto.NewPrimaryPhone))
            {
                return Results.BadRequest(new { message = "Հեռախոսահամարը պարտադիր է:" });
            }

            var cleanCurrent = System.Text.RegularExpressions.Regex.Replace(dto.CurrentPhone, @"\D", "");
            var specialists = await dbContext.Specialists.ToListAsync(ct);
            var specialist = specialists.FirstOrDefault(s =>
            {
                var sDigits = System.Text.RegularExpressions.Regex.Replace(s.Phone ?? "", @"\D", "");
                return cleanCurrent.Length >= 4 && (sDigits.EndsWith(cleanCurrent) || cleanCurrent.EndsWith(sDigits));
            });

            if (specialist == null)
            {
                return Results.NotFound(new { message = "Մասնագետը չի գտնվել:" });
            }

            // Check if there is already a Pending request
            var existingPending = await dbContext.SpecialistPhoneChangeRequests
                .FirstOrDefaultAsync(r => r.SpecialistId == specialist.Id && r.Status == "Pending", ct);

            if (existingPending != null)
            {
                return Results.Conflict(new { message = "Ձեր դիմումը գտնվում է նույնականացման և հաստատման փուլում։ Հաստատման ավարտից հետո Ձեր տվյալները կթարմացվեն և կարտացոլվեն համակարգում։", request = existingPending });
            }

            var newAddJson = dto.NewAdditionalPhonesJson;
            if (string.IsNullOrWhiteSpace(newAddJson) && dto.NewAdditionalPhones != null)
            {
                newAddJson = System.Text.Json.JsonSerializer.Serialize(dto.NewAdditionalPhones);
            }

            var request = new SpecialistPhoneChangeRequest(
                specialistId: specialist.Id,
                specialistName: specialist.Name,
                oldPrimaryPhone: specialist.Phone,
                oldAdditionalPhonesJson: specialist.AdditionalPhonesJson,
                newPrimaryPhone: dto.NewPrimaryPhone.Trim(),
                newAdditionalPhonesJson: newAddJson
            );

            dbContext.SpecialistPhoneChangeRequests.Add(request);
            await dbContext.SaveChangesAsync(ct);

            return Results.Ok(new { 
                message = "Ձեր դիմումը գտնվում է նույնականացման և հաստատման փուլում։ Հաստատման ավարտից հետո Ձեր տվյալները կթարմացվեն և կարտացոլվեն համակարգում։", 
                request 
            });
        })
        .WithSummary("Submit a phone change request from specialist profile");

        authGroup.MapGet("/phone-change-request/status", async ([FromQuery] string phone, AppDbContext dbContext, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return Results.Ok(new { hasPending = false });
            }

            var cleanDigits = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");
            var specialists = await dbContext.Specialists.ToListAsync(ct);
            var specialist = specialists.FirstOrDefault(s =>
            {
                var sDigits = System.Text.RegularExpressions.Regex.Replace(s.Phone ?? "", @"\D", "");
                return cleanDigits.Length >= 4 && (sDigits.EndsWith(cleanDigits) || cleanDigits.EndsWith(sDigits));
            });

            if (specialist == null)
            {
                return Results.Ok(new { hasPending = false });
            }

            var latest = await dbContext.SpecialistPhoneChangeRequests
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(r => r.SpecialistId == specialist.Id, ct);

            if (latest == null)
            {
                return Results.Ok(new { hasPending = false, hasRejected = false });
            }

            return Results.Ok(new { 
                hasPending = latest.Status == "Pending", 
                hasRejected = latest.Status == "Rejected",
                rejectionNote = latest.Status == "Rejected" ? latest.RejectionNote : null,
                request = latest 
            });
        })
        authGroup.MapPost("/update-avatar", async ([FromBody] UpdateAvatarRequestDto dto, AppDbContext dbContext, HttpContext httpContext, IWebHostEnvironment env, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Phone) || string.IsNullOrWhiteSpace(dto.AvatarUrl))
            {
                return Results.BadRequest(new { message = "Phone and avatarUrl are required." });
            }

            var cleanPhone = System.Text.RegularExpressions.Regex.Replace(dto.Phone, @"\D", "");
            var hostUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

            // Check if it's a specialist first
            var specialists = await dbContext.Specialists.ToListAsync(ct);
            var specialist = specialists.FirstOrDefault(s =>
            {
                var sDigits = System.Text.RegularExpressions.Regex.Replace(s.Phone ?? "", @"\D", "");
                return cleanPhone.Length >= 4 && (sDigits.EndsWith(cleanPhone) || cleanPhone.EndsWith(sDigits));
            });

            if (specialist != null)
            {
                var savedUrl = ImageStorageHelper.SaveBase64Image(dto.AvatarUrl, env.ContentRootPath, hostUrl, "specialists");
                specialist.Update(avatarUrl: savedUrl);
                await dbContext.SaveChangesAsync(ct);
                return Results.Ok(new { avatarUrl = savedUrl, message = "Avatar updated successfully." });
            }

            // Check if it's a regular user
            var users = await dbContext.Users.ToListAsync(ct);
            var user = users.FirstOrDefault(u =>
            {
                var uDigits = System.Text.RegularExpressions.Regex.Replace(u.Phone ?? "", @"\D", "");
                return cleanPhone.Length >= 4 && (uDigits.EndsWith(cleanPhone) || cleanPhone.EndsWith(uDigits));
            });

            if (user != null)
            {
                var savedUrl = ImageStorageHelper.SaveBase64Image(dto.AvatarUrl, env.ContentRootPath, hostUrl, "users");
                user.UpdateAvatar(savedUrl);
                await dbContext.SaveChangesAsync(ct);
                return Results.Ok(new { avatarUrl = savedUrl, message = "Avatar updated successfully." });
            }

            return Results.NotFound(new { message = "User/Specialist not found." });
        })
        .WithSummary("Update avatar photo for user or specialist");

        return app;
    }
}
