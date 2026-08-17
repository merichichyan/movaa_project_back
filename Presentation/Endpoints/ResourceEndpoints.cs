using System;
using System.Collections.Generic;
using System.Linq;
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
    public static class ResourceEndpoints
    {
        public static IEndpointRouteBuilder MapResourceEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/salon-resources")
                           .WithTags("Salon Resources");

            static async Task<Guid> ResolveSalonIdInternalAsync(Guid inputId, AppDbContext dbContext, CancellationToken ct)
            {
                var existsAsSalon = await dbContext.Salons.AnyAsync(s => s.Id == inputId, ct);
                if (existsAsSalon) return inputId;

                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == inputId, ct);
                if (user != null)
                {
                    var cleanUserPhone = System.Text.RegularExpressions.Regex.Replace(user.Phone ?? "", @"\D", "");
                    var salons = await dbContext.Salons.ToListAsync(ct);
                    var matched = salons.FirstOrDefault(s => {
                        var pDigits = System.Text.RegularExpressions.Regex.Replace(s.PhoneNumber ?? "", @"\D", "");
                        var oDigits = System.Text.RegularExpressions.Regex.Replace(s.OwnerPhoneNumber ?? "", @"\D", "");
                        return (cleanUserPhone.Length >= 4 && (pDigits.EndsWith(cleanUserPhone) || cleanUserPhone.EndsWith(pDigits) || oDigits.EndsWith(cleanUserPhone) || cleanUserPhone.EndsWith(oDigits)))
                               || (!string.IsNullOrWhiteSpace(s.Name) && s.Name.Equals(user.FullName, StringComparison.OrdinalIgnoreCase));
                    });
                    if (matched != null) return matched.Id;
                }
                return inputId;
            }

            // GET /api/salon-resources?salonId={salonId}
            group.MapGet("", async ([FromQuery] Guid? salonId, AppDbContext dbContext, CancellationToken ct) =>
            {
                var query = dbContext.SalonResources.AsQueryable();
                if (salonId.HasValue && salonId.Value != Guid.Empty)
                {
                    var resolvedSalonId = await ResolveSalonIdInternalAsync(salonId.Value, dbContext, ct);
                    query = query.Where(r => r.SalonId == resolvedSalonId);
                }

                var resources = await query.OrderBy(r => r.Name).ToListAsync(ct);
                return Results.Ok(resources);
            })
            .WithSummary("Get salon resources");

            // POST /api/salon-resources
            group.MapPost("", async ([FromBody] CreateSalonResourceDto dto, AppDbContext dbContext, CancellationToken ct) =>
            {
                if (dto.SalonId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Name))
                {
                    return Results.BadRequest(new { message = "SalonId and Name are required." });
                }

                var resolvedSalonId = await ResolveSalonIdInternalAsync(dto.SalonId, dbContext, ct);

                var resource = new SalonResource(
                    resolvedSalonId,
                    dto.Name,
                    dto.Quantity,
                    dto.Description,
                    dto.IsActive
                );

                dbContext.SalonResources.Add(resource);
                await dbContext.SaveChangesAsync(ct);

                return Results.Created($"/api/salon-resources/{resource.Id}", resource);
            })
            .WithSummary("Create a new salon resource");

            // PUT /api/salon-resources/{id:guid}
            group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateSalonResourceDto dto, AppDbContext dbContext, CancellationToken ct) =>
            {
                var resource = await dbContext.SalonResources.FirstOrDefaultAsync(r => r.Id == id, ct);
                if (resource == null)
                {
                    return Results.NotFound(new { message = "Salon resource not found." });
                }

                resource.Update(dto.Name, dto.Quantity, dto.Description, dto.IsActive);
                await dbContext.SaveChangesAsync(ct);

                return Results.Ok(resource);
            })
            .WithSummary("Update salon resource");

            // DELETE /api/salon-resources/{id:guid}
            group.MapDelete("/{id:guid}", async (Guid id, AppDbContext dbContext, CancellationToken ct) =>
            {
                var resource = await dbContext.SalonResources.FirstOrDefaultAsync(r => r.Id == id, ct);
                if (resource == null)
                {
                    return Results.NotFound(new { message = "Salon resource not found." });
                }

                // Remove service mappings referencing this resource
                var mappings = await dbContext.ServiceResources.Where(sr => sr.ResourceId == id).ToListAsync(ct);
                dbContext.ServiceResources.RemoveRange(mappings);

                dbContext.SalonResources.Remove(resource);
                await dbContext.SaveChangesAsync(ct);

                return Results.Ok(new { message = "Salon resource deleted successfully." });
            })
            .WithSummary("Delete salon resource");

            // POST /api/salon-resources/{id:guid}/toggle
            group.MapPost("/{id:guid}/toggle", async (Guid id, [FromBody] ToggleResourceStatusDto dto, AppDbContext dbContext, CancellationToken ct) =>
            {
                var resource = await dbContext.SalonResources.FirstOrDefaultAsync(r => r.Id == id, ct);
                if (resource == null)
                {
                    return Results.NotFound(new { message = "Salon resource not found." });
                }

                resource.SetActive(dto.IsActive);
                await dbContext.SaveChangesAsync(ct);

                return Results.Ok(resource);
            })
            .WithSummary("Toggle active status of a salon resource");

            // ---------------- Service Resource Mappings ----------------
            var serviceGroup = app.MapGroup("/api/service-resources")
                                  .WithTags("Service Resources");

            // GET /api/service-resources?salonId={salonId}&serviceId={serviceId}&serviceName={serviceName}
            serviceGroup.MapGet("", async ([FromQuery] Guid? salonId, [FromQuery] string? serviceId, [FromQuery] string? serviceName, AppDbContext dbContext, CancellationToken ct) =>
            {
                var query = dbContext.ServiceResources.Include(sr => sr.Resource).AsQueryable();

                if (salonId.HasValue && salonId.Value != Guid.Empty)
                {
                    query = query.Where(sr => sr.SalonId == salonId.Value);
                }

                if (!string.IsNullOrWhiteSpace(serviceId))
                {
                    var sIdLower = serviceId.Trim().ToLower();
                    query = query.Where(sr => sr.ServiceId.ToLower() == sIdLower || (sr.ServiceName != null && sr.ServiceName.ToLower() == sIdLower));
                }
                else if (!string.IsNullOrWhiteSpace(serviceName))
                {
                    var sNameLower = serviceName.Trim().ToLower();
                    query = query.Where(sr => (sr.ServiceName != null && sr.ServiceName.ToLower() == sNameLower) || sr.ServiceId.ToLower() == sNameLower);
                }

                var list = await query.Select(sr => new
                {
                    sr.Id,
                    sr.SalonId,
                    sr.ServiceId,
                    sr.ServiceName,
                    sr.ResourceId,
                    ResourceName = sr.Resource != null ? sr.Resource.Name : null,
                    IsResourceActive = sr.Resource != null && sr.Resource.IsActive,
                    sr.RequiredQuantity
                }).ToListAsync(ct);

                return Results.Ok(list);
            })
            .WithSummary("Get mapped required resources for services");

            // POST /api/service-resources/map
            serviceGroup.MapPost("/map", async ([FromBody] MapServiceResourcesDto dto, AppDbContext dbContext, CancellationToken ct) =>
            {
                var effectiveServiceId = !string.IsNullOrWhiteSpace(dto.ServiceId) ? dto.ServiceId.Trim() : dto.ServiceName?.Trim();

                if (dto.SalonId == Guid.Empty || string.IsNullOrWhiteSpace(effectiveServiceId))
                {
                    return Results.BadRequest(new { message = "SalonId and ServiceId (or ServiceName) are required." });
                }

                var sIdLower = effectiveServiceId.ToLower();

                // Remove existing mappings for this service in this salon
                var existing = await dbContext.ServiceResources
                    .Where(sr => sr.SalonId == dto.SalonId && (sr.ServiceId.ToLower() == sIdLower || (sr.ServiceName != null && sr.ServiceName.ToLower() == sIdLower)))
                    .ToListAsync(ct);

                dbContext.ServiceResources.RemoveRange(existing);

                var newMappings = new List<ServiceResource>();
                if (dto.RequiredResources != null)
                {
                    foreach (var item in dto.RequiredResources)
                    {
                        if (item.ResourceId != Guid.Empty && item.RequiredQuantity > 0)
                        {
                            newMappings.Add(new ServiceResource(
                                dto.SalonId,
                                effectiveServiceId,
                                item.ResourceId,
                                item.RequiredQuantity,
                                dto.ServiceName
                            ));
                        }
                    }
                }

                if (newMappings.Count > 0)
                {
                    dbContext.ServiceResources.AddRange(newMappings);
                }

                await dbContext.SaveChangesAsync(ct);

                return Results.Ok(new { message = "Service resources mapped successfully.", count = newMappings.Count });
            })
            .WithSummary("Map required resources to a service");

            return app;
        }
    }

    public record CreateSalonResourceDto(
        Guid SalonId,
        string Name,
        int Quantity,
        string? Description = null,
        bool IsActive = true
    );

    public record UpdateSalonResourceDto(
        string Name,
        int Quantity,
        string? Description = null,
        bool IsActive = true
    );

    public record ToggleResourceStatusDto(
        bool IsActive
    );

    public record RequiredResourceItemDto(
        Guid ResourceId,
        int RequiredQuantity
    );

    public record MapServiceResourcesDto(
        Guid SalonId,
        string? ServiceId,
        string? ServiceName,
        List<RequiredResourceItemDto>? RequiredResources
    );
}
