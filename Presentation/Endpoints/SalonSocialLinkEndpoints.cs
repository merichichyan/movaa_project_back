using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movaa_project_back.Application.DTOs.Specialist;
using movaa_project_back.Application.Services;
using movaa_project_back.Data;
using movaa_project_back.Domain.Entities;
using movaa_project_back.Domain.Enums;

namespace movaa_project_back.Presentation.Endpoints;

public static class SalonSocialLinkEndpoints
{
    public static IEndpointRouteBuilder MapSalonSocialLinkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/salons").WithTags("Salon Social Links");

        // 1. GET /api/salons/{salonId}/social-links
        group.MapGet("/{salonId}/social-links", async (Guid salonId, AppDbContext dbContext, CancellationToken ct) =>
        {
            var links = await dbContext.SalonSocialLinks
                .Where(sl => sl.SalonId == salonId)
                .OrderBy(sl => sl.DisplayOrder)
                .ThenBy(sl => sl.CreatedAt)
                .Select(sl => new
                {
                    sl.Id,
                    sl.SalonId,
                    Platform = sl.Platform.ToString(),
                    sl.Url,
                    sl.DisplayOrder,
                    sl.CreatedAt,
                    sl.UpdatedAt
                })
                .ToListAsync(ct);

            return Results.Ok(links);
        })
        .WithSummary("Get social links for a salon");

        // 2. POST /api/salons/{salonId}/social-links
        group.MapPost("/{salonId}/social-links", async (Guid salonId, [FromBody] CreateSocialLinkDto dto, AppDbContext dbContext, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Url))
            {
                return Results.BadRequest(new { message = "URL is required." });
            }

            string normalizedUrl;
            try
            {
                normalizedUrl = SocialMediaService.NormalizeUrl(dto.Url);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }

            SocialPlatform platform;
            if (!string.IsNullOrWhiteSpace(dto.Platform) && Enum.TryParse<SocialPlatform>(dto.Platform, true, out var parsedPlatform))
            {
                platform = parsedPlatform;
            }
            else
            {
                platform = SocialMediaService.DetectPlatform(normalizedUrl);
            }

            var existingCount = await dbContext.SalonSocialLinks.CountAsync(sl => sl.SalonId == salonId, ct);
            var displayOrder = dto.DisplayOrder ?? existingCount;

            var duplicate = await dbContext.SalonSocialLinks.AnyAsync(sl => sl.SalonId == salonId && sl.Platform == platform, ct);
            if (duplicate)
            {
                return Results.Conflict(new { message = $"A link for platform '{platform}' already exists for this salon." });
            }

            var link = new SalonSocialLink(salonId, platform, normalizedUrl, displayOrder);
            dbContext.SalonSocialLinks.Add(link);
            await dbContext.SaveChangesAsync(ct);

            var result = new
            {
                link.Id,
                SalonId = link.SalonId,
                Platform = link.Platform.ToString(),
                link.Url,
                link.DisplayOrder,
                link.CreatedAt,
                link.UpdatedAt
            };

            return Results.Created($"/api/salons/{salonId}/social-links/{link.Id}", result);
        })
        .WithSummary("Create a social link for a salon");

        // 3. DELETE /api/salons/{salonId}/social-links/{linkId}
        group.MapDelete("/{salonId}/social-links/{linkId}", async (Guid salonId, Guid linkId, AppDbContext dbContext, CancellationToken ct) =>
        {
            var link = await dbContext.SalonSocialLinks.FirstOrDefaultAsync(sl => sl.Id == linkId && sl.SalonId == salonId, ct);
            if (link == null)
            {
                return Results.NotFound(new { message = "Social link not found." });
            }

            dbContext.SalonSocialLinks.Remove(link);
            await dbContext.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Social link deleted successfully." });
        })
        .WithSummary("Delete a social link for a salon");

        return app;
    }
}
