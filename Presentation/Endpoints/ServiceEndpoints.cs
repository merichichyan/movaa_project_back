using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using movaa_project_back.Data;
using movaa_project_back.Domain.Entities;

namespace movaa_project_back.Presentation.Endpoints
{
    public record CreateServiceDto(
        string Name,
        double Price,
        string? Category,
        int? DurationMinutes,
        Guid? SalonId,
        string? NameHy,
        string? NameEn,
        string? NameRu,
        string? Description,
        List<string>? SpecialistIds,
        bool? IsActive
    );

    public record UpdateServiceDto(
        string Name,
        double Price,
        string? Category,
        int? DurationMinutes,
        Guid? SalonId,
        string? NameHy,
        string? NameEn,
        string? NameRu,
        string? Description,
        List<string>? SpecialistIds,
        bool? IsActive
    );

    public static class ServiceEndpoints
    {
        // Bi-directional sync helper: Service -> Specialists
        public static async Task SyncServiceToSpecialistsAsync(ServiceItem service, List<string>? selectedSpecIdStrs, AppDbContext dbContext, CancellationToken ct)
        {
            selectedSpecIdStrs ??= new List<string>();
            var targetSpecGuids = selectedSpecIdStrs
                .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();

            // Save specialistIds on ServiceItem
            service.SetSpecialistIdsJson(JsonSerializer.Serialize(targetSpecGuids.Select(g => g.ToString())));

            var allSpecialists = await dbContext.Specialists.ToListAsync(ct);

            foreach (var sp in allSpecialists)
            {
                List<Dictionary<string, object>> list = new();
                try
                {
                    if (!string.IsNullOrWhiteSpace(sp.ServicesJson))
                    {
                        list = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(sp.ServicesJson) ?? new();
                    }
                }
                catch { }

                bool isTarget = targetSpecGuids.Contains(sp.Id);
                var sIdStr = service.Id.ToString();
                var sNameLower = service.Name.Trim().ToLower();

                int existingIdx = list.FindIndex(item =>
                {
                    var itemId = item.TryGetValue("id", out var idVal) ? idVal?.ToString() : null;
                    var itemName = item.TryGetValue("name", out var nameVal) ? nameVal?.ToString() : null;
                    return itemId == sIdStr || (itemName != null && itemName.Trim().ToLower() == sNameLower);
                });

                if (isTarget)
                {
                    var serviceDict = new Dictionary<string, object>
                    {
                        ["id"] = sIdStr,
                        ["name"] = service.Name,
                        ["nameHy"] = service.NameHy,
                        ["price"] = service.Price,
                        ["duration"] = service.DurationMinutes,
                        ["category"] = service.Category
                    };

                    if (existingIdx >= 0)
                    {
                        list[existingIdx] = serviceDict;
                    }
                    else
                    {
                        list.Add(serviceDict);
                    }
                }
                else
                {
                    if (existingIdx >= 0)
                    {
                        list.RemoveAt(existingIdx);
                    }
                }

                sp.Update(
                    name: sp.Name,
                    category: sp.Category,
                    phone: sp.Phone,
                    nameHy: sp.NameHy,
                    nameEn: sp.NameEn,
                    nameRu: sp.NameRu,
                    jobTitle: sp.JobTitle,
                    jobTitleHy: sp.JobTitleHy,
                    jobTitleEn: sp.JobTitleEn,
                    jobTitleRu: sp.JobTitleRu,
                    email: sp.Email,
                    salonId: sp.SalonId,
                    salonName: sp.SalonName,
                    avatarUrl: sp.AvatarUrl,
                    bio: sp.Bio,
                    bioHy: sp.BioHy,
                    bioEn: sp.BioEn,
                    bioRu: sp.BioRu,
                    experienceYears: sp.ExperienceYears,
                    workingHours: sp.WorkingHours,
                    commissionRate: sp.CommissionRate,
                    servicesJson: JsonSerializer.Serialize(list),
                    workplacesJson: sp.WorkplacesJson
                );
            }
        }

        // Bi-directional sync helper: Specialist -> Services
        public static async Task SyncSpecialistToServicesAsync(Specialist specialist, AppDbContext dbContext, CancellationToken ct)
        {
            if (specialist == null) return;

            List<Dictionary<string, object>> specServices = new();
            try
            {
                if (!string.IsNullOrWhiteSpace(specialist.ServicesJson))
                {
                    specServices = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(specialist.ServicesJson) ?? new();
                }
            }
            catch { }

            var spIdStr = specialist.Id.ToString();
            var allServices = await dbContext.Services.ToListAsync(ct);

            foreach (var srv in allServices)
            {
                List<string> linkedSpecIds = new();
                try
                {
                    if (!string.IsNullOrWhiteSpace(srv.SpecialistIdsJson))
                    {
                        linkedSpecIds = JsonSerializer.Deserialize<List<string>>(srv.SpecialistIdsJson) ?? new();
                    }
                }
                catch { }

                var srvIdStr = srv.Id.ToString();
                var srvNameLower = srv.Name.Trim().ToLower();

                bool shouldBeLinked = specServices.Any(item =>
                {
                    var itemId = item.TryGetValue("id", out var idVal) ? idVal?.ToString() : null;
                    var itemName = item.TryGetValue("name", out var nameVal) ? nameVal?.ToString() : null;
                    return itemId == srvIdStr || (itemName != null && itemName.Trim().ToLower() == srvNameLower);
                });

                if (shouldBeLinked)
                {
                    if (!linkedSpecIds.Contains(spIdStr))
                    {
                        linkedSpecIds.Add(spIdStr);
                        srv.SetSpecialistIdsJson(JsonSerializer.Serialize(linkedSpecIds));
                    }
                }
                else
                {
                    if (linkedSpecIds.Contains(spIdStr))
                    {
                        linkedSpecIds.Remove(spIdStr);
                        srv.SetSpecialistIdsJson(JsonSerializer.Serialize(linkedSpecIds));
                    }
                }
            }
        }

        public static IEndpointRouteBuilder MapServiceEndpoints(this IEndpointRouteBuilder app)
        {
            var apiGroup = app.MapGroup("/api/services").WithTags("Services");
            var adminGroup = app.MapGroup("/api/admin/services").WithTags("Services Admin");

            // Map Service response DTO
            static object MapServiceResponse(ServiceItem s, AppDbContext dbContext)
            {
                List<string> specIds = new();
                try
                {
                    if (!string.IsNullOrWhiteSpace(s.SpecialistIdsJson))
                    {
                        specIds = JsonSerializer.Deserialize<List<string>>(s.SpecialistIdsJson) ?? new();
                    }
                }
                catch { }

                return new
                {
                    id = s.Id.ToString(),
                    salonId = s.SalonId?.ToString(),
                    name = s.Name,
                    nameHy = s.NameHy,
                    nameEn = s.NameEn,
                    nameRu = s.NameRu,
                    category = s.Category,
                    price = s.Price,
                    durationMinutes = s.DurationMinutes,
                    description = s.Description,
                    specialistIds = specIds,
                    isActive = s.IsActive,
                    createdAt = s.CreatedAt,
                    updatedAt = s.UpdatedAt
                };
            }

            // GET /api/services & /api/admin/services
            async Task<IResult> GetServicesHandler(Guid? salonId, Guid? specialistId, string? category, bool? activeOnly, AppDbContext dbContext, CancellationToken ct)
            {
                try
                {
                    var query = dbContext.Services.AsQueryable();
                    if (activeOnly ?? false)
                    {
                        query = query.Where(s => s.IsActive);
                    }

                    if (salonId.HasValue && salonId.Value != Guid.Empty)
                    {
                        query = query.Where(s => s.SalonId == salonId.Value || s.SalonId == null);
                    }

                    if (!string.IsNullOrWhiteSpace(category))
                    {
                        var catLower = category.Trim().ToLower();
                        query = query.Where(s => s.Category.ToLower() == catLower);
                    }

                    var list = await query.OrderBy(s => s.Category).ThenBy(s => s.Name).ToListAsync(ct);

                    if (specialistId.HasValue && specialistId.Value != Guid.Empty)
                    {
                        var spIdStr = specialistId.Value.ToString();
                        list = list.Where(s =>
                        {
                            try
                            {
                                var ids = JsonSerializer.Deserialize<List<string>>(s.SpecialistIdsJson ?? "[]");
                                return ids != null && ids.Contains(spIdStr);
                            }
                            catch { return false; }
                        }).ToList();
                    }

                    var res = list.Select(s => MapServiceResponse(s, dbContext)).ToList();
                    return Results.Ok(res);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GetServices Error: {ex.Message}");
                    return Results.Ok(new List<object>());
                }
            }

            apiGroup.MapGet("", GetServicesHandler);
            adminGroup.MapGet("", GetServicesHandler);

            // POST /api/services & /api/admin/services
            async Task<IResult> CreateServiceHandler([FromBody] CreateServiceDto dto, AppDbContext dbContext, CancellationToken ct)
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return Results.BadRequest(new { message = "Ծառայության անվանումը պարտադիր է (Service name is required)." });
                }

                try
                {
                    var service = new ServiceItem(
                        name: dto.Name,
                        price: dto.Price,
                        category: dto.Category ?? "General",
                        durationMinutes: dto.DurationMinutes ?? 30,
                        salonId: dto.SalonId,
                        nameHy: dto.NameHy,
                        nameEn: dto.NameEn,
                        nameRu: dto.NameRu,
                        description: dto.Description,
                        isActive: dto.IsActive ?? true
                    );

                    dbContext.Services.Add(service);
                    await SyncServiceToSpecialistsAsync(service, dto.SpecialistIds, dbContext, ct);

                    await dbContext.SaveChangesAsync(ct);
                    return Results.Created($"/api/services/{service.Id}", MapServiceResponse(service, dbContext));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CreateService Error: {ex.Message}");
                    return Results.BadRequest(new { message = ex.Message });
                }
            }

            apiGroup.MapPost("", CreateServiceHandler);
            adminGroup.MapPost("", CreateServiceHandler);

            // PUT /api/services/{id:guid} & /api/admin/services/{id:guid}
            async Task<IResult> UpdateServiceHandler(Guid id, [FromBody] UpdateServiceDto dto, AppDbContext dbContext, CancellationToken ct)
            {
                try
                {
                    var service = await dbContext.Services.FirstOrDefaultAsync(s => s.Id == id, ct);
                    if (service == null) return Results.NotFound(new { message = "Ծառայությունը չի գտնվել (Service not found)." });

                    service.Update(
                        name: dto.Name,
                        price: dto.Price,
                        category: dto.Category ?? "General",
                        durationMinutes: dto.DurationMinutes ?? 30,
                        nameHy: dto.NameHy,
                        nameEn: dto.NameEn,
                        nameRu: dto.NameRu,
                        description: dto.Description,
                        isActive: dto.IsActive,
                        salonId: dto.SalonId
                    );

                    await SyncServiceToSpecialistsAsync(service, dto.SpecialistIds, dbContext, ct);

                    await dbContext.SaveChangesAsync(ct);
                    return Results.Ok(MapServiceResponse(service, dbContext));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"UpdateService Error: {ex.Message}");
                    return Results.BadRequest(new { message = ex.Message });
                }
            }

            apiGroup.MapPut("/{id:guid}", UpdateServiceHandler);
            adminGroup.MapPut("/{id:guid}", UpdateServiceHandler);

            // DELETE /api/services/{id:guid} & /api/admin/services/{id:guid}
            async Task<IResult> DeleteServiceHandler(Guid id, AppDbContext dbContext, CancellationToken ct)
            {
                try
                {
                    var service = await dbContext.Services.FirstOrDefaultAsync(s => s.Id == id, ct);
                    if (service == null) return Results.NotFound(new { message = "Ծառայությունը չի գտնվել (Service not found)." });

                    // Remove service links from all specialists
                    await SyncServiceToSpecialistsAsync(service, new List<string>(), dbContext, ct);

                    dbContext.Services.Remove(service);
                    await dbContext.SaveChangesAsync(ct);

                    return Results.Ok(new { message = "Ծառայությունը հաջողությամբ ջնջվեց (Service deleted successfully)." });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DeleteService Error: {ex.Message}");
                    return Results.BadRequest(new { message = ex.Message });
                }
            }

            apiGroup.MapDelete("/{id:guid}", DeleteServiceHandler);
            adminGroup.MapDelete("/{id:guid}", DeleteServiceHandler);

            return app;
        }
    }
}
