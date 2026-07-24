using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movaa_project_back.Application.DTOs.Admin;
using movaa_project_back.Data;
using movaa_project_back.Domain.Entities;

namespace movaa_project_back.Presentation.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/api/admin").WithTags("Admin");

        // ------------------ USERS MANAGEMENT ------------------
        adminGroup.MapGet("/users", async (AppDbContext dbContext, CancellationToken ct) =>
        {
            var users = await dbContext.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    u.Id,
                    u.Phone,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.Status,
                    u.IsBlocked,
                    u.IsOnboardingCompleted,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .ToListAsync(ct);
            return Results.Ok(users);
        })
        .WithSummary("Get all users");

        adminGroup.MapPost("/users/{id:guid}/password", async (Guid id, [FromBody] ChangePasswordRequestDto dto, AppDbContext dbContext, CancellationToken ct) =>
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user == null) return Results.NotFound(new { message = "User not found." });

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            {
                return Results.BadRequest(new { message = "Password must be at least 6 characters long." });
            }

            var newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatePasswordHash(newHash);
            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Password updated successfully." });
        })
        .WithSummary("Change user password by admin");

        adminGroup.MapPost("/users/{id:guid}/block", async (Guid id, [FromBody] BlockToggleDto dto, AppDbContext dbContext, CancellationToken ct) =>
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user == null) return Results.NotFound(new { message = "User not found." });

            user.SetBlocked(dto.IsBlocked);
            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(new { message = dto.IsBlocked ? "User blocked successfully." : "User unblocked successfully.", isBlocked = user.IsBlocked });
        })
        .WithSummary("Block or unblock a user");

        // ------------------ SALONS MANAGEMENT ------------------
        adminGroup.MapGet("/salons", async (AppDbContext dbContext, CancellationToken ct) =>
        {
            var salons = await dbContext.Salons
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(ct);
            return Results.Ok(salons);
        })
        .WithSummary("Get all salons (Admin view)");

        adminGroup.MapPost("/salons", async ([FromBody] CreateSalonDto dto, AppDbContext dbContext, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Address) || string.IsNullOrWhiteSpace(dto.Phone))
            {
                return Results.BadRequest(new { message = "Name, address, and phone are required." });
            }

            var salon = new Salon(
                name: dto.Name,
                address: dto.Address,
                phone: dto.Phone,
                email: dto.Email,
                description: dto.Description,
                logoUrl: dto.LogoUrl
            );

            dbContext.Salons.Add(salon);
            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(salon);
        })
        .WithSummary("Create a new salon");

        adminGroup.MapPut("/salons/{id:guid}", async (Guid id, [FromBody] UpdateSalonDto dto, AppDbContext dbContext, CancellationToken ct) =>
        {
            var salon = await dbContext.Salons.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (salon == null) return Results.NotFound(new { message = "Salon not found." });

            salon.Update(
                name: dto.Name,
                address: dto.Address,
                phone: dto.Phone,
                email: dto.Email,
                description: dto.Description,
                logoUrl: dto.LogoUrl
            );

            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(salon);
        })
        .WithSummary("Update salon details");

        adminGroup.MapPost("/salons/{id:guid}/block", async (Guid id, [FromBody] BlockToggleDto dto, AppDbContext dbContext, CancellationToken ct) =>
        {
            var salon = await dbContext.Salons.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (salon == null) return Results.NotFound(new { message = "Salon not found." });

            salon.SetBlocked(dto.IsBlocked);
            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(new { message = dto.IsBlocked ? "Salon blocked successfully." : "Salon unblocked successfully.", isBlocked = salon.IsBlocked });
        })
        .WithSummary("Block or unblock a salon");

        // ------------------ SPECIALISTS MANAGEMENT ------------------
        adminGroup.MapGet("/specialists", async (AppDbContext dbContext, CancellationToken ct) =>
        {
            var specialists = await dbContext.Specialists
                .OrderByDescending(sp => sp.CreatedAt)
                .ToListAsync(ct);
            return Results.Ok(specialists);
        })
        .WithSummary("Get all specialists (Admin view)");

        adminGroup.MapPost("/specialists", async ([FromBody] CreateSpecialistDto dto, AppDbContext dbContext, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Category) || string.IsNullOrWhiteSpace(dto.Phone))
            {
                return Results.BadRequest(new { message = "Name, category, and phone are required." });
            }

            var specialist = new Specialist(
                name: dto.Name,
                category: dto.Category,
                phone: dto.Phone,
                email: dto.Email,
                salonId: dto.SalonId,
                salonName: dto.SalonName,
                avatarUrl: dto.AvatarUrl
            );

            dbContext.Specialists.Add(specialist);
            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(specialist);
        })
        .WithSummary("Create a new specialist");

        adminGroup.MapPut("/specialists/{id:guid}", async (Guid id, [FromBody] UpdateSpecialistDto dto, AppDbContext dbContext, CancellationToken ct) =>
        {
            var specialist = await dbContext.Specialists.FirstOrDefaultAsync(sp => sp.Id == id, ct);
            if (specialist == null) return Results.NotFound(new { message = "Specialist not found." });

            specialist.Update(
                name: dto.Name,
                category: dto.Category,
                phone: dto.Phone,
                email: dto.Email,
                salonId: dto.SalonId,
                salonName: dto.SalonName,
                avatarUrl: dto.AvatarUrl
            );

            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(specialist);
        })
        .WithSummary("Update specialist details");

        adminGroup.MapPost("/specialists/{id:guid}/block", async (Guid id, [FromBody] BlockToggleDto dto, AppDbContext dbContext, CancellationToken ct) =>
        {
            var specialist = await dbContext.Specialists.FirstOrDefaultAsync(sp => sp.Id == id, ct);
            if (specialist == null) return Results.NotFound(new { message = "Specialist not found." });

            specialist.SetBlocked(dto.IsBlocked);
            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(new { message = dto.IsBlocked ? "Specialist blocked successfully." : "Specialist unblocked successfully.", isBlocked = specialist.IsBlocked });
        })
        .WithSummary("Block or unblock a specialist");

        // ------------------ PUBLIC ENDPOINTS FOR USER APP ------------------
        var publicGroup = app.MapGroup("/api").WithTags("Public");

        publicGroup.MapGet("/salons", async (AppDbContext dbContext, CancellationToken ct) =>
        {
            var activeSalons = await dbContext.Salons
                .Where(s => !s.IsBlocked)
                .OrderByDescending(s => s.Rating)
                .ToListAsync(ct);
            return Results.Ok(activeSalons);
        })
        .WithSummary("Get active salons for user app");

        publicGroup.MapGet("/specialists", async (AppDbContext dbContext, CancellationToken ct) =>
        {
            var activeSpecialists = await dbContext.Specialists
                .Where(sp => !sp.IsBlocked)
                .OrderByDescending(sp => sp.Rating)
                .ToListAsync(ct);
            return Results.Ok(activeSpecialists);
        })
        .WithSummary("Get active specialists for user app");

        return app;
    }
}
