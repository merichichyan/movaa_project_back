using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movaa_project_back.Application.DTOs.Admin;
using movaa_project_back.Application.Services;
using movaa_project_back.Data;
using movaa_project_back.Domain.Entities;

namespace movaa_project_back.Presentation.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/api").WithTags("Admin");

        // ------------------ USERS MANAGEMENT ------------------
        adminGroup.MapGet("/users", async (AppDbContext dbContext, CancellationToken ct) =>
        {
            try
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching users: {ex}");
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""IsBlocked"" boolean DEFAULT false;
                        ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""IsOnboardingCompleted"" boolean DEFAULT false;
                    ", ct);
                    var retryUsers = await dbContext.Users.OrderByDescending(u => u.CreatedAt).Select(u => new { u.Id, u.Phone, u.FullName, u.Email, u.Role, u.Status, u.IsBlocked, u.IsOnboardingCompleted, u.CreatedAt, u.UpdatedAt }).ToListAsync(ct);
                    return Results.Ok(retryUsers);
                }
                catch
                {
                    return Results.Ok(new List<object>());
                }
            }
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
            try
            {
                var salons = await dbContext.Salons
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync(ct);

                return Results.Ok(salons);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching salons from database: {ex}");
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""IsBlocked"" boolean DEFAULT false;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""OwnerFullName"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""OwnerPhoneNumber"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""TaxId"" text;
                    ", ct);
                    var retrySalons = await dbContext.Salons.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
                    return Results.Ok(retrySalons);
                }
                catch
                {
                    return Results.Ok(new List<object>());
                }
            }
        })
        .WithSummary("Get all salons");

        adminGroup.MapPost("/salons", async ([FromBody] CreateSalonDto dto, AppDbContext dbContext, HttpContext httpContext, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var phoneVal = !string.IsNullOrWhiteSpace(dto.PhoneNumber) ? dto.PhoneNumber : (!string.IsNullOrWhiteSpace(dto.Phone) ? dto.Phone : "+37400000000");
            var ownerPhoneVal = !string.IsNullOrWhiteSpace(dto.OwnerPhoneNumber) ? dto.OwnerPhoneNumber : (!string.IsNullOrWhiteSpace(dto.OwnerPhone) ? dto.OwnerPhone : phoneVal);
            var ownerNameVal = !string.IsNullOrWhiteSpace(dto.OwnerFullName) ? dto.OwnerFullName : (!string.IsNullOrWhiteSpace(dto.OwnerName) ? dto.OwnerName : dto.Name);
            var categoryVal = !string.IsNullOrWhiteSpace(dto.Category) ? dto.Category : "Salon";
            var workingHoursVal = !string.IsNullOrWhiteSpace(dto.WorkingHours) ? dto.WorkingHours : "09:00 - 18:00";
            var taxIdVal = !string.IsNullOrWhiteSpace(dto.TaxId) ? dto.TaxId : "00000000";

            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Address) || string.IsNullOrWhiteSpace(phoneVal))
            {
                return Results.BadRequest(new { message = "Name, address, and phone number are required." });
            }

            var hostUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var savedLogoUrl = ImageStorageHelper.SaveBase64Image(dto.LogoUrl, env.ContentRootPath, hostUrl);

            var salon = new Salon(
                name: dto.Name,
                address: dto.Address,
                phoneNumber: phoneVal,
                nameHy: dto.NameHy,
                nameEn: dto.NameEn,
                nameRu: dto.NameRu,
                addressHy: dto.AddressHy,
                addressEn: dto.AddressEn,
                addressRu: dto.AddressRu,
                category: categoryVal,
                workingHours: workingHoursVal,
                email: dto.Email,
                description: dto.Description,
                descriptionHy: dto.DescriptionHy,
                descriptionEn: dto.DescriptionEn,
                descriptionRu: dto.DescriptionRu,
                logoUrl: savedLogoUrl,
                ownerFullName: ownerNameVal,
                ownerNameHy: dto.OwnerNameHy,
                ownerNameEn: dto.OwnerNameEn,
                ownerNameRu: dto.OwnerNameRu,
                ownerPhoneNumber: ownerPhoneVal,
                taxId: taxIdVal
            );

            try
            {
                dbContext.Salons.Add(salon);
                await dbContext.SaveChangesAsync(ct);
                return Results.Ok(salon);
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"Error creating salon: {ex} -> {innerMessage}");

                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""PhoneNumber"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""Category"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""WorkingHours"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""Name"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""NameHy"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""NameEn"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""NameRu"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""Address"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""AddressHy"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""AddressEn"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""AddressRu"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""Email"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""Description"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""DescriptionHy"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""DescriptionEn"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""DescriptionRu"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""LogoUrl"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""OwnerFullName"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""OwnerNameHy"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""OwnerNameEn"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""OwnerNameRu"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""OwnerPhoneNumber"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""TaxId"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""IsBlocked"" boolean DEFAULT false;
                    ", ct);

                    await dbContext.SaveChangesAsync(ct);
                    return Results.Ok(salon);
                }
                catch
                {
                    return Results.Problem(detail: $"Error creating salon: {innerMessage}", statusCode: 500);
                }
            }
        })
        .WithSummary("Create a new salon");

        adminGroup.MapPut("/salons/{id:guid}", async (Guid id, [FromBody] UpdateSalonDto dto, AppDbContext dbContext, HttpContext httpContext, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var salon = await dbContext.Salons.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (salon == null) return Results.NotFound(new { message = "Salon not found." });

            var hostUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var savedLogoUrl = ImageStorageHelper.SaveBase64Image(dto.LogoUrl, env.ContentRootPath, hostUrl);

            var phoneVal = !string.IsNullOrWhiteSpace(dto.PhoneNumber) ? dto.PhoneNumber : dto.Phone;
            var ownerPhoneVal = !string.IsNullOrWhiteSpace(dto.OwnerPhoneNumber) ? dto.OwnerPhoneNumber : dto.OwnerPhone;
            var ownerNameVal = !string.IsNullOrWhiteSpace(dto.OwnerFullName) ? dto.OwnerFullName : dto.OwnerName;

            salon.Update(
                name: dto.Name,
                address: dto.Address,
                phoneNumber: phoneVal ?? salon.PhoneNumber,
                nameHy: dto.NameHy,
                nameEn: dto.NameEn,
                nameRu: dto.NameRu,
                addressHy: dto.AddressHy,
                addressEn: dto.AddressEn,
                addressRu: dto.AddressRu,
                category: !string.IsNullOrWhiteSpace(dto.Category) ? dto.Category : salon.Category,
                workingHours: !string.IsNullOrWhiteSpace(dto.WorkingHours) ? dto.WorkingHours : salon.WorkingHours,
                email: dto.Email,
                description: dto.Description,
                descriptionHy: dto.DescriptionHy,
                descriptionEn: dto.DescriptionEn,
                descriptionRu: dto.DescriptionRu,
                logoUrl: savedLogoUrl,
                ownerFullName: ownerNameVal,
                ownerNameHy: dto.OwnerNameHy,
                ownerNameEn: dto.OwnerNameEn,
                ownerNameRu: dto.OwnerNameRu,
                ownerPhoneNumber: ownerPhoneVal,
                taxId: dto.TaxId
            );

            try
            {
                await dbContext.SaveChangesAsync(ct);
                return Results.Ok(salon);
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"Error updating salon: {ex} -> {innerMessage}");

                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""IsBlocked"" boolean DEFAULT false;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""OwnerFullName"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""OwnerPhoneNumber"" text;
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""TaxId"" text;
                    ", ct);

                    await dbContext.SaveChangesAsync(ct);
                    return Results.Ok(salon);
                }
                catch
                {
                    return Results.Problem(detail: $"Error updating salon: {innerMessage}", statusCode: 500);
                }
            }
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
            try
            {
                var specialists = await dbContext.Specialists
                    .OrderByDescending(sp => sp.CreatedAt)
                    .Select(sp => new
                    {
                        sp.Id,
                        sp.Name,
                        sp.NameHy,
                        sp.NameEn,
                        sp.NameRu,
                        sp.JobTitle,
                        sp.JobTitleHy,
                        sp.JobTitleEn,
                        sp.JobTitleRu,
                        sp.Category,
                        sp.Phone,
                        sp.Email,
                        sp.SalonId,
                        sp.SalonName,
                        sp.AvatarUrl,
                        sp.Bio,
                        sp.BioHy,
                        sp.BioEn,
                        sp.BioRu,
                        sp.ExperienceYears,
                        sp.WorkingHours,
                        sp.CommissionRate,
                        sp.ServicesJson,
                        sp.Rating,
                        sp.ReviewCount,
                        sp.IsBlocked,
                        sp.CreatedAt,
                        sp.UpdatedAt
                    })
                    .ToListAsync(ct);
                return Results.Ok(specialists);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching specialists: {ex}");
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""JobTitle"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""Bio"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""ExperienceYears"" integer DEFAULT 0;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""WorkingHours"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""CommissionRate"" double precision DEFAULT 0.0;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""ServicesJson"" text DEFAULT '[]';
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""IsBlocked"" boolean DEFAULT false;
                    ", ct);
                    var retrySpecialists = await dbContext.Specialists
                        .OrderByDescending(sp => sp.CreatedAt)
                        .Select(sp => new
                        {
                            sp.Id,
                            sp.Name,
                            sp.JobTitle,
                            sp.Category,
                            sp.Phone,
                            sp.Email,
                            sp.SalonId,
                            sp.SalonName,
                            sp.AvatarUrl,
                            sp.Bio,
                            sp.ExperienceYears,
                            sp.WorkingHours,
                            sp.CommissionRate,
                            sp.ServicesJson,
                            sp.Rating,
                            sp.ReviewCount,
                            sp.IsBlocked,
                            sp.CreatedAt,
                            sp.UpdatedAt
                        })
                        .ToListAsync(ct);
                    return Results.Ok(retrySpecialists);
                }
                catch
                {
                    return Results.Ok(new List<object>());
                }
            }
        })
        .WithSummary("Get all specialists (Admin view)");

        adminGroup.MapPost("/specialists", async ([FromBody] CreateSpecialistDto dto, AppDbContext dbContext, HttpContext httpContext, IWebHostEnvironment env, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Category) || string.IsNullOrWhiteSpace(dto.Phone))
            {
                return Results.BadRequest(new { message = "Name, category, and phone are required." });
            }

            var hostUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var savedAvatarUrl = ImageStorageHelper.SaveBase64Image(dto.AvatarUrl, env.ContentRootPath, hostUrl);

            var specialist = new Specialist(
                name: dto.Name,
                category: dto.Category,
                phone: dto.Phone,
                nameHy: dto.NameHy,
                nameEn: dto.NameEn,
                nameRu: dto.NameRu,
                jobTitle: dto.JobTitle,
                jobTitleHy: dto.JobTitleHy,
                jobTitleEn: dto.JobTitleEn,
                jobTitleRu: dto.JobTitleRu,
                email: dto.Email,
                salonId: dto.SalonId,
                salonName: dto.SalonName,
                avatarUrl: savedAvatarUrl,
                bio: dto.Bio,
                bioHy: dto.BioHy,
                bioEn: dto.BioEn,
                bioRu: dto.BioRu,
                experienceYears: dto.ExperienceYears ?? 0,
                workingHours: dto.WorkingHours,
                commissionRate: dto.CommissionRate ?? 0.0,
                servicesJson: dto.ServicesJson
            );

            try
            {
                dbContext.Specialists.Add(specialist);
                await dbContext.SaveChangesAsync(ct);
                return Results.Ok(specialist);
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"Error creating specialist: {ex} -> {innerMessage}");

                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""JobTitle"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""JobTitleHy"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""JobTitleEn"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""JobTitleRu"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""NameHy"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""NameEn"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""NameRu"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""Bio"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""BioHy"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""BioEn"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""BioRu"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""ExperienceYears"" integer DEFAULT 0;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""WorkingHours"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""CommissionRate"" double precision DEFAULT 0.0;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""ServicesJson"" text DEFAULT '[]';
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""IsBlocked"" boolean DEFAULT false;
                    ", ct);

                    await dbContext.SaveChangesAsync(ct);
                    return Results.Ok(specialist);
                }
                catch
                {
                    return Results.Problem(detail: $"Error creating specialist: {innerMessage}", statusCode: 500);
                }
            }
        })
        .WithSummary("Create a new specialist");

        adminGroup.MapPut("/specialists/{id:guid}", async (Guid id, [FromBody] UpdateSpecialistDto dto, AppDbContext dbContext, HttpContext httpContext, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var specialist = await dbContext.Specialists.FirstOrDefaultAsync(sp => sp.Id == id, ct);
            if (specialist == null) return Results.NotFound(new { message = "Specialist not found." });

            var hostUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var savedAvatarUrl = ImageStorageHelper.SaveBase64Image(dto.AvatarUrl, env.ContentRootPath, hostUrl);

            specialist.Update(
                name: dto.Name,
                category: dto.Category,
                phone: dto.Phone,
                nameHy: dto.NameHy,
                nameEn: dto.NameEn,
                nameRu: dto.NameRu,
                jobTitle: dto.JobTitle,
                jobTitleHy: dto.JobTitleHy,
                jobTitleEn: dto.JobTitleEn,
                jobTitleRu: dto.JobTitleRu,
                email: dto.Email,
                salonId: dto.SalonId,
                salonName: dto.SalonName,
                avatarUrl: savedAvatarUrl,
                bio: dto.Bio,
                bioHy: dto.BioHy,
                bioEn: dto.BioEn,
                bioRu: dto.BioRu,
                experienceYears: dto.ExperienceYears ?? 0,
                workingHours: dto.WorkingHours,
                commissionRate: dto.CommissionRate ?? 0.0,
                servicesJson: dto.ServicesJson
            );

            try
            {
                await dbContext.SaveChangesAsync(ct);
                return Results.Ok(specialist);
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"Error updating specialist: {ex} -> {innerMessage}");

                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""JobTitle"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""Bio"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""ExperienceYears"" integer DEFAULT 0;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""WorkingHours"" text;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""CommissionRate"" double precision DEFAULT 0.0;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""ServicesJson"" text DEFAULT '[]';
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""IsBlocked"" boolean DEFAULT false;
                    ", ct);

                    await dbContext.SaveChangesAsync(ct);
                    return Results.Ok(specialist);
                }
                catch
                {
                    return Results.Problem(detail: $"Error updating specialist: {innerMessage}", statusCode: 500);
                }
            }
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

        // ------------------ CATEGORIES MANAGEMENT ------------------
        adminGroup.MapGet("/categories", async (AppDbContext dbContext, CancellationToken ct) =>
        {
            try
            {
                var categories = await dbContext.Categories
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.CreatedAt)
                    .ToListAsync(ct);
                return Results.Ok(categories);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching categories: {ex}");
                return Results.Ok(new List<object>());
            }
        })
        .WithSummary("Get all categories");

        adminGroup.MapPost("/categories", async ([FromBody] CreateCategoryDto dto, AppDbContext dbContext, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.NameHy))
            {
                return Results.BadRequest(new { message = "Armenian category name (NameHy) is required." });
            }

            var category = new Category(
                nameHy: dto.NameHy,
                nameEn: !string.IsNullOrWhiteSpace(dto.NameEn) ? dto.NameEn : dto.NameHy,
                nameRu: !string.IsNullOrWhiteSpace(dto.NameRu) ? dto.NameRu : dto.NameHy,
                iconName: dto.IconName ?? "grid_view_rounded",
                displayOrder: dto.DisplayOrder ?? 0
            );

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(category);
        })
        .WithSummary("Create a new category");

        adminGroup.MapPut("/categories/{id:guid}", async (Guid id, [FromBody] UpdateCategoryDto dto, AppDbContext dbContext, CancellationToken ct) =>
        {
            var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (category == null) return Results.NotFound(new { message = "Category not found." });

            category.Update(
                nameHy: dto.NameHy,
                nameEn: !string.IsNullOrWhiteSpace(dto.NameEn) ? dto.NameEn : dto.NameHy,
                nameRu: !string.IsNullOrWhiteSpace(dto.NameRu) ? dto.NameRu : dto.NameHy,
                iconName: dto.IconName ?? category.IconName,
                displayOrder: dto.DisplayOrder ?? category.DisplayOrder,
                isActive: dto.IsActive
            );

            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(category);
        })
        .WithSummary("Update category details");

        adminGroup.MapDelete("/categories/{id:guid}", async (Guid id, AppDbContext dbContext, CancellationToken ct) =>
        {
            var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (category == null) return Results.NotFound(new { message = "Category not found." });

            dbContext.Categories.Remove(category);
            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Category deleted successfully." });
        })
        .WithSummary("Delete a category");

        return app;
    }
}
