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

        // ------------------ ADMIN AUTHENTICATION ------------------
        adminGroup.MapPost("/admin/login", async ([FromBody] AdminLoginRequestDto dto, AppDbContext dbContext, movaa_project_back.Domain.Interfaces.IJwtTokenGenerator tokenGenerator, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return Results.BadRequest(new { message = "Մուտքանունը և գաղտնաբառը պարտադիր են:" });
            }

            var username = dto.Username.Trim();
            var password = dto.Password.Trim();

            // 1. Check dedicated Admins table first
            var admin = await dbContext.Admins.FirstOrDefaultAsync(a => a.Username.ToLower() == username.ToLower(), ct);
            if (admin != null && BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            {
                var token = tokenGenerator.GenerateAdminToken(admin);
                return Results.Ok(new
                {
                    token,
                    user = new
                    {
                        id = admin.Id,
                        username = admin.Username,
                        fullName = admin.FullName,
                        email = admin.Email,
                        role = admin.Role
                    }
                });
            }

            // 2. Fallback check in Users table for legacy admin credentials
            var userAdmin = await dbContext.Users.FirstOrDefaultAsync(u => u.Phone.ToLower() == username.ToLower(), ct);
            if (userAdmin != null && userAdmin.Role == "admin" && BCrypt.Net.BCrypt.Verify(password, userAdmin.PasswordHash))
            {
                var token = tokenGenerator.GenerateToken(userAdmin);
                return Results.Ok(new
                {
                    token,
                    user = new
                    {
                        id = userAdmin.Id,
                        username = userAdmin.Phone,
                        fullName = userAdmin.FullName,
                        email = userAdmin.Email,
                        role = userAdmin.Role
                    }
                });
            }

            return Results.Unauthorized();
        })
        .WithSummary("Admin Login")
        .WithDescription("Authenticates rootadmin or administrators using dedicated Admins table.");

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

        adminGroup.MapPut("/users/{id}", async (string id, [FromBody] UpdateUserDto dto, AppDbContext dbContext, CancellationToken ct) =>
        {
            User? user = null;
            if (Guid.TryParse(id, out var parsedGuid))
            {
                user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == parsedGuid, ct);
            }

            if (user == null && !string.IsNullOrWhiteSpace(dto.Phone))
            {
                var cleanPhone = System.Text.RegularExpressions.Regex.Replace(dto.Phone, @"\D", "");
                if (cleanPhone.Length >= 6)
                {
                    var allUsers = await dbContext.Users.ToListAsync(ct);
                    user = allUsers.FirstOrDefault(u =>
                    {
                        var uDigits = System.Text.RegularExpressions.Regex.Replace(u.Phone ?? "", @"\D", "");
                        return uDigits.Length >= 6 && uDigits.EndsWith(cleanPhone.Substring(Math.Max(0, cleanPhone.Length - 6)));
                    });
                }
            }

            if (user == null) return Results.NotFound(new { message = "User not found." });

            var oldPhone = user.Phone;
            var oldEmail = user.Email;

            var cleanNewPhone = System.Text.RegularExpressions.Regex.Replace(dto.Phone ?? "", @"\D", "");
            var formattedPhone = cleanNewPhone.StartsWith("374") ? "+" + cleanNewPhone : "+374" + cleanNewPhone.TrimStart('0');
            var cleanEmail = dto.Email?.Trim().ToLowerInvariant();

            user.UpdateProfile(formattedPhone, dto.FullName?.Trim() ?? user.FullName, cleanEmail, user.Gender, user.Birthday);

            // Sync matching Specialist entity if exists
            var specialists = await dbContext.Specialists.ToListAsync(ct);
            var cleanOldPhoneDigits = System.Text.RegularExpressions.Regex.Replace(oldPhone ?? "", @"\D", "");
            var specialist = specialists.FirstOrDefault(sp =>
            {
                var spDigits = System.Text.RegularExpressions.Regex.Replace(sp.Phone ?? "", @"\D", "");
                if (cleanOldPhoneDigits.Length >= 8 && spDigits.Length >= 8 && spDigits.EndsWith(cleanOldPhoneDigits.Substring(cleanOldPhoneDigits.Length - 8))) return true;
                if (!string.IsNullOrEmpty(oldEmail) && !string.IsNullOrEmpty(sp.Email) && sp.Email.ToLowerInvariant() == oldEmail.ToLowerInvariant()) return true;
                return false;
            });

            if (specialist != null)
            {
                specialist.UpdatePhones(formattedPhone, specialist.AdditionalPhonesJson);
                specialist.Update(
                    dto.FullName?.Trim() ?? specialist.Name,
                    specialist.Category,
                    formattedPhone,
                    specialist.NameHy,
                    specialist.NameEn,
                    specialist.NameRu,
                    specialist.JobTitle,
                    specialist.JobTitleHy,
                    specialist.JobTitleEn,
                    specialist.JobTitleRu,
                    cleanEmail,
                    specialist.SalonId,
                    specialist.SalonName,
                    specialist.AvatarUrl,
                    specialist.Bio,
                    specialist.BioHy,
                    specialist.BioEn,
                    specialist.BioRu,
                    specialist.ExperienceYears,
                    specialist.WorkingHours,
                    specialist.CommissionRate,
                    specialist.ServicesJson,
                    specialist.WorkplacesJson
                );
            }

            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(new { message = "User updated successfully.", user });
        })
        .WithSummary("Update user profile (phone, email, full name) by admin");

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
            var savedLogoUrl = ImageStorageHelper.SaveBase64Image(dto.LogoUrl, env.ContentRootPath, hostUrl, "salons");

            var createAddJson = dto.AdditionalPhonesJson;
            if (string.IsNullOrWhiteSpace(createAddJson) && dto.AdditionalPhones != null)
            {
                createAddJson = System.Text.Json.JsonSerializer.Serialize(dto.AdditionalPhones);
            }

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
                taxId: taxIdVal,
                additionalPhonesJson: createAddJson
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
                        ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""AdditionalPhonesJson"" text DEFAULT '[]';
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
            var savedLogoUrl = ImageStorageHelper.SaveBase64Image(dto.LogoUrl, env.ContentRootPath, hostUrl, "salons");

            var phoneVal = !string.IsNullOrWhiteSpace(dto.PhoneNumber) ? dto.PhoneNumber : dto.Phone;
            var ownerPhoneVal = !string.IsNullOrWhiteSpace(dto.OwnerPhoneNumber) ? dto.OwnerPhoneNumber : dto.OwnerPhone;
            var ownerNameVal = !string.IsNullOrWhiteSpace(dto.OwnerFullName) ? dto.OwnerFullName : dto.OwnerName;

            var updateAddJson = dto.AdditionalPhonesJson;
            if (string.IsNullOrWhiteSpace(updateAddJson) && dto.AdditionalPhones != null)
            {
                updateAddJson = System.Text.Json.JsonSerializer.Serialize(dto.AdditionalPhones);
            }

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
                taxId: dto.TaxId,
                additionalPhonesJson: updateAddJson
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

        // Note: Branches management endpoints are registered in OrganizationEndpoints.cs to prevent ambiguous route collisions.

        // ------------------ SPECIALISTS MANAGEMENT ------------------
        async Task<IResult> GetSpecialistsHandler(AppDbContext dbContext, CancellationToken ct, bool activeOnly = false, Guid? salonId = null)
        {
            try
            {
                var query = dbContext.Specialists.AsQueryable();

                // When activeOnly=true (client view), only return specialists who have activated their account and are not blocked
                if (activeOnly)
                {
                    query = query.Where(sp => !sp.IsBlocked);
                }

                if (salonId.HasValue)
                {
                    var sId = salonId.Value;
                    var salonObj = await dbContext.Salons.FirstOrDefaultAsync(s => s.Id == sId, ct);
                    var orgObj = salonObj == null ? await dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == sId, ct) : null;
                    var targetName = salonObj?.Name ?? orgObj?.Name;

                    var linkedSpecIds = await dbContext.SpecialistBranches
                        .Where(sb => sb.OrganizationId == sId || sb.BranchId == sId)
                        .Select(sb => sb.SpecialistId)
                        .ToListAsync(ct);

                    if (!string.IsNullOrWhiteSpace(targetName))
                    {
                        var cleanTarget = targetName.Trim().ToLower();
                        query = query.Where(sp => 
                            sp.SalonId == sId 
                            || linkedSpecIds.Contains(sp.Id)
                            || (sp.SalonName != null && (sp.SalonName.ToLower().Contains(cleanTarget) || cleanTarget.Contains(sp.SalonName.ToLower())))
                        );
                    }
                    else
                    {
                        query = query.Where(sp => sp.SalonId == sId || linkedSpecIds.Contains(sp.Id));
                    }
                }

                var specialists = await query
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
                        sp.AdditionalPhonesJson,
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
                        sp.WorkplacesJson,
                        sp.Rating,
                        sp.ReviewCount,
                        sp.IsBlocked,
                        sp.IsActivated,
                        sp.CreatedAt,
                        sp.UpdatedAt,
                        socialLinks = dbContext.SpecialistSocialLinks
                            .Where(sl => sl.SpecialistId == sp.Id)
                            .OrderBy(sl => sl.DisplayOrder)
                            .Select(sl => new
                            {
                                sl.Id,
                                sl.SpecialistId,
                                Platform = sl.Platform.ToString(),
                                sl.Url,
                                sl.DisplayOrder,
                                sl.CreatedAt,
                                sl.UpdatedAt
                            }).ToList()
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
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""WorkplacesJson"" text DEFAULT '[]';
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""IsBlocked"" boolean DEFAULT false;
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""IsActivated"" boolean DEFAULT false;
                    ", ct);
                    var retryQuery = dbContext.Specialists.AsQueryable();
                    if (activeOnly) retryQuery = retryQuery.Where(sp => !sp.IsBlocked);
                    if (salonId.HasValue)
                    {
                        var sId = salonId.Value;
                        var salonObj = await dbContext.Salons.FirstOrDefaultAsync(s => s.Id == sId, ct);
                        var orgObj = salonObj == null ? await dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == sId, ct) : null;
                        var targetName = salonObj?.Name ?? orgObj?.Name;

                        var linkedSpecIds = await dbContext.SpecialistBranches
                            .Where(sb => sb.OrganizationId == sId || sb.BranchId == sId)
                            .Select(sb => sb.SpecialistId)
                            .ToListAsync(ct);

                        if (!string.IsNullOrWhiteSpace(targetName))
                        {
                            var cleanTarget = targetName.Trim().ToLower();
                            retryQuery = retryQuery.Where(sp => 
                                sp.SalonId == sId 
                                || linkedSpecIds.Contains(sp.Id)
                                || (sp.SalonName != null && (sp.SalonName.ToLower().Contains(cleanTarget) || cleanTarget.Contains(sp.SalonName.ToLower())))
                            );
                        }
                        else
                        {
                            retryQuery = retryQuery.Where(sp => sp.SalonId == sId || linkedSpecIds.Contains(sp.Id));
                        }
                    }
                    var retrySpecialists = await retryQuery
                        .OrderByDescending(sp => sp.CreatedAt)
                        .Select(sp => new
                        {
                            sp.Id,
                            sp.Name,
                            sp.JobTitle,
                            sp.Category,
                            sp.Phone,
                            sp.AdditionalPhonesJson,
                            sp.Email,
                            sp.SalonId,
                            sp.SalonName,
                            sp.AvatarUrl,
                            sp.Bio,
                            sp.ExperienceYears,
                            sp.WorkingHours,
                            sp.CommissionRate,
                            sp.ServicesJson,
                            sp.WorkplacesJson,
                            sp.Rating,
                            sp.ReviewCount,
                            sp.IsBlocked,
                            sp.IsActivated,
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
        }

        app.MapGet("/api/specialists", async (AppDbContext dbContext, CancellationToken ct, [FromQuery] bool? activeOnly, [FromQuery] Guid? salonId) => await GetSpecialistsHandler(dbContext, ct, activeOnly: activeOnly ?? true, salonId: salonId));
        adminGroup.MapGet("/admin/specialists", async (AppDbContext dbContext, CancellationToken ct, [FromQuery] bool? activeOnly, [FromQuery] Guid? salonId) => await GetSpecialistsHandler(dbContext, ct, activeOnly: activeOnly ?? false, salonId: salonId));

        adminGroup.MapPost("/specialists", async ([FromBody] CreateSpecialistDto dto, AppDbContext dbContext, HttpContext httpContext, IWebHostEnvironment env, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Category) || string.IsNullOrWhiteSpace(dto.Phone))
            {
                return Results.BadRequest(new { message = "Name, category, and phone are required." });
            }

            var hostUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var savedAvatarUrl = ImageStorageHelper.SaveBase64Image(dto.AvatarUrl, env.ContentRootPath, hostUrl, "specialists");

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
                servicesJson: dto.ServicesJson,
                workplacesJson: dto.WorkplacesJson
            );

            var createAddJson = dto.AdditionalPhonesJson;
            if (string.IsNullOrWhiteSpace(createAddJson) && dto.AdditionalPhones != null)
            {
                createAddJson = System.Text.Json.JsonSerializer.Serialize(dto.AdditionalPhones);
            }
            if (!string.IsNullOrWhiteSpace(createAddJson))
            {
                specialist.UpdatePhones(dto.Phone, createAddJson);
            }

            try
            {
                dbContext.Specialists.Add(specialist);
                await ServiceEndpoints.SyncSpecialistToServicesAsync(specialist, dbContext, ct);
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
                        ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""WorkplacesJson"" text DEFAULT '[]';
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
            var savedAvatarUrl = ImageStorageHelper.SaveBase64Image(dto.AvatarUrl, env.ContentRootPath, hostUrl, "specialists");

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
                servicesJson: dto.ServicesJson,
                workplacesJson: dto.WorkplacesJson
            );

            var updateAddJson = dto.AdditionalPhonesJson;
            if (string.IsNullOrWhiteSpace(updateAddJson) && dto.AdditionalPhones != null)
            {
                updateAddJson = System.Text.Json.JsonSerializer.Serialize(dto.AdditionalPhones);
            }
            specialist.UpdatePhones(dto.Phone, updateAddJson);

            // Sync matching User entity's email if matching User exists
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var newEmailClean = dto.Email.Trim().ToLowerInvariant();
                var cleanPhone = System.Text.RegularExpressions.Regex.Replace(dto.Phone ?? specialist.Phone ?? "", @"\D", "");
                var users = await dbContext.Users.ToListAsync(ct);
                var user = users.FirstOrDefault(u =>
                {
                    var uDigits = System.Text.RegularExpressions.Regex.Replace(u.Phone ?? "", @"\D", "");
                    if (cleanPhone.Length >= 8 && uDigits.Length >= 8 && uDigits.EndsWith(cleanPhone.Substring(cleanPhone.Length - 8))) return true;
                    if (!string.IsNullOrEmpty(specialist.Email) && !string.IsNullOrEmpty(u.Email) && u.Email.ToLowerInvariant() == specialist.Email.ToLowerInvariant()) return true;
                    return false;
                });
                if (user != null)
                {
                    user.UpdateProfile(user.Phone, dto.Name ?? user.FullName, newEmailClean, user.Gender, user.Birthday);
                }
            }

            try
            {
                await ServiceEndpoints.SyncSpecialistToServicesAsync(specialist, dbContext, ct);
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

        // ------------------ SPECIALIST SOCIAL LINKS ------------------
        adminGroup.MapGet("/specialists/{specialistId}/social-links", async (string specialistId, AppDbContext dbContext, CancellationToken ct) =>
        {
            try
            {
                if (!Guid.TryParse(specialistId, out var specGuid))
                {
                    return Results.BadRequest(new { message = $"Invalid specialist ID format: '{specialistId}'" });
                }

                var specialist = await dbContext.Specialists.FirstOrDefaultAsync(s => s.Id == specGuid, ct);
                if (specialist == null) return Results.NotFound(new { message = $"Specialist with ID {specialistId} not found." });

                var links = await dbContext.SpecialistSocialLinks
                    .Where(sl => sl.SpecialistId == specGuid)
                    .OrderBy(sl => sl.DisplayOrder)
                    .ThenBy(sl => sl.CreatedAt)
                    .Select(sl => new
                    {
                        sl.Id,
                        sl.SpecialistId,
                        Platform = sl.Platform.ToString(),
                        sl.Url,
                        sl.DisplayOrder,
                        sl.CreatedAt,
                        sl.UpdatedAt
                    })
                    .ToListAsync(ct);

                return Results.Ok(links);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SocialLinks GET Error]: {ex.Message} -> {ex.StackTrace}");
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS ""SpecialistSocialLinks"" (
                            ""Id"" UUID PRIMARY KEY,
                            ""SpecialistId"" UUID NOT NULL,
                            ""Platform"" TEXT NOT NULL,
                            ""Url"" TEXT NOT NULL,
                            ""DisplayOrder"" INT DEFAULT 0,
                            ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                            ""UpdatedAt"" TIMESTAMP WITH TIME ZONE
                        );
                    ", ct);
                    return Results.Ok(new List<object>());
                }
                catch (Exception dbEx)
                {
                    return Results.Problem(detail: $"Database Error: {dbEx.Message}", statusCode: 500);
                }
            }
        })
        .WithSummary("Get social links for a specialist");

        adminGroup.MapPost("/specialists/{specialistId}/social-links", async (string specialistId, [FromBody] movaa_project_back.Application.DTOs.Specialist.CreateSocialLinkDto dto, System.Security.Claims.ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken ct) =>
        {
            try
            {
                if (!Guid.TryParse(specialistId, out var specGuid))
                {
                    return Results.BadRequest(new { message = $"Invalid specialist ID format: '{specialistId}'" });
                }

                var specialist = await dbContext.Specialists.FirstOrDefaultAsync(s => s.Id == specGuid, ct);
                if (specialist == null) return Results.NotFound(new { message = $"Specialist with ID {specialistId} not found in database." });

                if (string.IsNullOrWhiteSpace(dto.Url)) return Results.BadRequest(new { message = "URL is required." });

                string normalizedUrl;
                try
                {
                    normalizedUrl = SocialMediaService.NormalizeUrl(dto.Url);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }

                movaa_project_back.Domain.Enums.SocialPlatform platform;
                if (!string.IsNullOrWhiteSpace(dto.Platform) && Enum.TryParse<movaa_project_back.Domain.Enums.SocialPlatform>(dto.Platform, true, out var parsedPlatform))
                {
                    platform = parsedPlatform;
                }
                else
                {
                    platform = SocialMediaService.DetectPlatform(normalizedUrl);
                }

                var existingCount = await dbContext.SpecialistSocialLinks.CountAsync(sl => sl.SpecialistId == specGuid, ct);
                var displayOrder = dto.DisplayOrder ?? existingCount;

                var duplicate = await dbContext.SpecialistSocialLinks.AnyAsync(sl => sl.SpecialistId == specGuid && sl.Platform == platform, ct);
                if (duplicate)
                {
                    return Results.Conflict(new { message = $"A link for platform '{platform}' already exists for this specialist." });
                }

                var link = new SpecialistSocialLink(specGuid, platform, normalizedUrl, displayOrder);
                dbContext.SpecialistSocialLinks.Add(link);
                await dbContext.SaveChangesAsync(ct);

                var result = new movaa_project_back.Application.DTOs.Specialist.SocialLinkDto(link.Id, link.SpecialistId, link.Platform.ToString(), link.Url, link.DisplayOrder, link.CreatedAt, link.UpdatedAt);
                return Results.Created($"/api/specialists/{specGuid}/social-links/{link.Id}", result);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"[SocialLinks POST Error]: {ex.Message} -> {innerMsg}");
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS ""SpecialistSocialLinks"" (
                            ""Id"" UUID PRIMARY KEY,
                            ""SpecialistId"" UUID NOT NULL,
                            ""Platform"" TEXT NOT NULL,
                            ""Url"" TEXT NOT NULL,
                            ""DisplayOrder"" INT DEFAULT 0,
                            ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                            ""UpdatedAt"" TIMESTAMP WITH TIME ZONE
                        );
                    ", ct);
                }
                catch { }
                return Results.Problem(detail: $"Error saving social link: {innerMsg}", statusCode: 500);
            }
        })
        .WithSummary("Create a social link for a specialist");

        adminGroup.MapDelete("/specialists/{specialistId}/social-links/{linkId}", async (string specialistId, string linkId, AppDbContext dbContext, CancellationToken ct) =>
        {
            try
            {
                if (!Guid.TryParse(specialistId, out var specGuid) || !Guid.TryParse(linkId, out var linkGuid))
                {
                    return Results.BadRequest(new { message = "Invalid ID format." });
                }

                var link = await dbContext.SpecialistSocialLinks.FirstOrDefaultAsync(sl => sl.Id == linkGuid && sl.SpecialistId == specGuid, ct);
                if (link == null) return Results.NotFound(new { message = "Social link not found." });

                dbContext.SpecialistSocialLinks.Remove(link);
                await dbContext.SaveChangesAsync(ct);
                return Results.Ok(new { message = "Social link deleted successfully." });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: $"Error deleting social link: {ex.Message}", statusCode: 500);
            }
        })
        .WithSummary("Delete a social link for a specialist");

        adminGroup.MapPost("/specialists/{id:guid}/password", async (Guid id, [FromBody] ChangePasswordRequestDto dto, AppDbContext dbContext, CancellationToken ct) =>
        {
            var specialist = await dbContext.Specialists.FirstOrDefaultAsync(sp => sp.Id == id, ct);
            if (specialist == null) return Results.NotFound(new { message = "Specialist not found." });

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            {
                return Results.BadRequest(new { message = "Password must be at least 6 characters long." });
            }

            var newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword.Trim());

            var rawPhone = specialist.Phone;
            var cleanDigits = System.Text.RegularExpressions.Regex.Replace(rawPhone ?? "", @"\D", "");
            var phoneFormatted = cleanDigits.StartsWith("374") ? "+" + cleanDigits : "+374" + cleanDigits.TrimStart('0');
            var userEmail = specialist.Email?.Trim().ToLowerInvariant();

            var users = await dbContext.Users.ToListAsync(ct);
            var user = users.FirstOrDefault(u =>
            {
                var uDigits = System.Text.RegularExpressions.Regex.Replace(u.Phone ?? "", @"\D", "");
                if (cleanDigits.Length >= 8 && uDigits.Length >= 8 && uDigits.EndsWith(cleanDigits.Substring(cleanDigits.Length - 8))) return true;
                if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(u.Email) && u.Email.ToLowerInvariant() == userEmail) return true;
                return false;
            });

            if (user != null)
            {
                user.UpdatePasswordHash(newHash);
                user.UpdateRole("specialist");
            }
            else
            {
                user = new User(
                    phone: phoneFormatted,
                    passwordHash: newHash,
                    fullName: specialist.Name,
                    role: "specialist",
                    email: userEmail
                );
                user.UpdateStatus("Verified");
                dbContext.Users.Add(user);
            }

            specialist.SetActivated();
            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Specialist password updated successfully." });
        })
        .WithSummary("Change specialist password by admin");

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

        // Phone Change Requests Management (Specialist & Salon)
        adminGroup.MapGet("/phone-requests", GetPhoneChangeRequests);
        adminGroup.MapGet("/phone-change-requests", GetPhoneChangeRequests);
        adminGroup.MapGet("/specialist-phone-requests", GetPhoneChangeRequests);

        adminGroup.MapPost("/phone-requests/{id:guid}/approve", ApprovePhoneChangeRequest);
        adminGroup.MapPost("/phone-change-requests/{id:guid}/approve", ApprovePhoneChangeRequest);
        adminGroup.MapPost("/specialist-phone-requests/{id:guid}/approve", ApprovePhoneChangeRequest);

        adminGroup.MapPost("/phone-requests/{id:guid}/reject", RejectPhoneChangeRequest);
        adminGroup.MapPost("/phone-change-requests/{id:guid}/reject", RejectPhoneChangeRequest);
        adminGroup.MapPost("/specialist-phone-requests/{id:guid}/reject", RejectPhoneChangeRequest);

        async Task<IResult> GetPhoneChangeRequests(AppDbContext dbContext, CancellationToken ct)
        {
            var requests = await dbContext.SpecialistPhoneChangeRequests
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(ct);

            var salonIds = await dbContext.Salons.Select(s => s.Id).ToListAsync(ct);
            var specialistIds = await dbContext.Specialists.Select(s => s.Id).ToListAsync(ct);

            var result = requests.Select(r => new
            {
                r.Id,
                r.SpecialistId,
                r.SpecialistName,
                r.OldPrimaryPhone,
                r.OldAdditionalPhonesJson,
                r.NewPrimaryPhone,
                r.NewAdditionalPhonesJson,
                r.Status,
                r.RejectionNote,
                r.CreatedAt,
                r.UpdatedAt,
                Role = salonIds.Contains(r.SpecialistId) ? "SALON" : (specialistIds.Contains(r.SpecialistId) ? "SPECIALIST" : "USER")
            });

            return Results.Ok(result);
        }

        async Task<IResult> ApprovePhoneChangeRequest(Guid id, AppDbContext dbContext, CancellationToken ct)
        {
            var request = await dbContext.SpecialistPhoneChangeRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (request == null) return Results.NotFound(new { message = "Request not found." });

            var cleanNew = System.Text.RegularExpressions.Regex.Replace(request.NewPrimaryPhone ?? "", @"\D", "");
            var formattedNew = cleanNew.StartsWith("374") ? "+" + cleanNew : "+374" + cleanNew.TrimStart('0');

            var specialist = await dbContext.Specialists.FirstOrDefaultAsync(s => s.Id == request.SpecialistId, ct);
            if (specialist != null)
            {
                specialist.UpdatePhones(formattedNew, request.NewAdditionalPhonesJson);
            }
            else
            {
                var salon = await dbContext.Salons.FirstOrDefaultAsync(s => s.Id == request.SpecialistId, ct);
                if (salon != null)
                {
                    salon.UpdatePhones(formattedNew, request.NewAdditionalPhonesJson);
                }
            }

            // Sync User phone if matching User exists
            var cleanOld = System.Text.RegularExpressions.Regex.Replace(request.OldPrimaryPhone ?? "", @"\D", "");
            var users = await dbContext.Users.ToListAsync(ct);
            var user = users.FirstOrDefault(u =>
            {
                var uDigits = System.Text.RegularExpressions.Regex.Replace(u.Phone ?? "", @"\D", "");
                return cleanOld.Length >= 6 && uDigits.EndsWith(cleanOld);
            });
            if (user != null)
            {
                user.UpdatePhone(formattedNew);
            }

            request.Approve();
            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Phone change request approved successfully.", request });
        }

        async Task<IResult> RejectPhoneChangeRequest(Guid id, [FromBody] RejectPhoneRequestDto? body, AppDbContext dbContext, CancellationToken ct)
        {
            var request = await dbContext.SpecialistPhoneChangeRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (request == null) return Results.NotFound(new { message = "Request not found." });

            request.Reject(body?.Note, body?.NoteHy, body?.NoteEn, body?.NoteRu);
            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Phone change request rejected.", request });
        }

        return app;
    }
}
