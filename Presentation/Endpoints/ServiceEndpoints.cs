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

                bool modified = false;

                if (isTarget)
                {
                    var serviceDict = new Dictionary<string, object>
                    {
                        ["id"] = sIdStr,
                        ["name"] = service.Name,
                        ["nameHy"] = service.NameHy ?? service.Name,
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
                    modified = true;
                }
                else
                {
                    if (existingIdx >= 0)
                    {
                        list.RemoveAt(existingIdx);
                        modified = true;
                    }
                }

                if (modified)
                {
                    sp.UpdateServicesJson(JsonSerializer.Serialize(list));
                }
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

        private static bool _migrationAttempted = false;

        private static async Task EnsureServiceColumnsExistAsync(AppDbContext dbContext, CancellationToken ct, bool force = false)
        {
            if (_migrationAttempted && !force) return;
            _migrationAttempted = true;
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS ""Services"" (
                        ""Id"" uuid PRIMARY KEY,
                        ""SalonId"" uuid,
                        ""Name"" text NOT NULL DEFAULT '',
                        ""NameHy"" text,
                        ""NameEn"" text,
                        ""NameRu"" text,
                        ""Category"" text NOT NULL DEFAULT 'General',
                        ""Price"" double precision NOT NULL DEFAULT 0,
                        ""DurationMinutes"" integer NOT NULL DEFAULT 30,
                        ""Description"" text,
                        ""SpecialistIdsJson"" text NOT NULL DEFAULT '[]',
                        ""IsActive"" boolean NOT NULL DEFAULT true,
                        ""CreatedAt"" timestamp with time zone DEFAULT NOW(),
                        ""UpdatedAt"" timestamp with time zone
                    );
                    ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""Name"" text DEFAULT '';
                    ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""Price"" double precision DEFAULT 0;
                    ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""Category"" text DEFAULT 'General';
                    ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""NameHy"" text;
                    ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""NameEn"" text;
                    ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""NameRu"" text;
                    ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""SalonId"" uuid;
                    ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""DurationMinutes"" integer DEFAULT 30;
                    ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""Description"" text;
                    ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""SpecialistIdsJson"" text DEFAULT '[]';
                    ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""IsActive"" boolean DEFAULT true;
                    ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" timestamp with time zone DEFAULT NOW();
                    ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""UpdatedAt"" timestamp with time zone;
                ", ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EnsureServiceColumnsExist notice: {ex.Message}");
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

            static async Task<List<object>> FetchServicesListAsync(Guid? salonId, Guid? specialistId, string? category, bool? activeOnly, AppDbContext dbContext, CancellationToken ct)
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

                return list.Select(s => MapServiceResponse(s, dbContext)).ToList();
            }

            // GET /api/services & /api/admin/services
            async Task<IResult> GetServicesHandler(Guid? salonId, Guid? specialistId, string? category, bool? activeOnly, AppDbContext dbContext, CancellationToken ct)
            {
                await EnsureServiceColumnsExistAsync(dbContext, ct);
                try
                {
                    var res = await FetchServicesListAsync(salonId, specialistId, category, activeOnly, dbContext, ct);
                    return Results.Ok(res);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GetServices Primary Error: {ex.Message}. Retrying after force migration...");
                    await EnsureServiceColumnsExistAsync(dbContext, ct, force: true);
                    try
                    {
                        var res = await FetchServicesListAsync(salonId, specialistId, category, activeOnly, dbContext, ct);
                        return Results.Ok(res);
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"GetServices Retry Error: {ex2.Message}");
                        return Results.Ok(new List<object>());
                    }
                }
            }

            apiGroup.MapGet("", GetServicesHandler);
            adminGroup.MapGet("", GetServicesHandler);

            // POST /api/services & /api/admin/services
            async Task<IResult> CreateServiceHandler([FromBody] CreateServiceDto dto, AppDbContext dbContext, CancellationToken ct)
            {
                await EnsureServiceColumnsExistAsync(dbContext, ct);
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
                    var msg = ex.InnerException?.Message ?? ex.Message;
                    Console.WriteLine($"CreateService Error: {ex.Message} | Inner: {ex.InnerException?.Message}");
                    return Results.BadRequest(new { message = msg });
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
                    var msg = ex.InnerException?.Message ?? ex.Message;
                    Console.WriteLine($"UpdateService Error: {ex.Message} | Inner: {ex.InnerException?.Message}");
                    return Results.BadRequest(new { message = msg });
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
                    var msg = ex.InnerException?.Message ?? ex.Message;
                    Console.WriteLine($"DeleteService Error: {ex.Message} | Inner: {ex.InnerException?.Message}");
                    return Results.BadRequest(new { message = msg });
                }
            }

            apiGroup.MapDelete("/{id:guid}", DeleteServiceHandler);
            adminGroup.MapDelete("/{id:guid}", DeleteServiceHandler);

            return app;
        }
    }
}
