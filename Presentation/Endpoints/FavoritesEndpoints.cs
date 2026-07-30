using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using movaa_project_back.Data;
using movaa_project_back.Domain.Entities;

namespace movaa_project_back.Presentation.Endpoints
{
    public static class FavoritesEndpoints
    {
        public static IEndpointRouteBuilder MapFavoritesEndpoints(this IEndpointRouteBuilder app)
        {
            var favoritesGroup = app.MapGroup("/api/favorites").WithTags("Favorites");

            favoritesGroup.MapGet("", [Authorize] async (ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken ct) =>
            {
                var nameIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(nameIdClaim) || !Guid.TryParse(nameIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var favorites = await dbContext.UserFavorites
                    .Where(uf => uf.UserId == userId)
                    .ToListAsync(ct);

                var salonIds = favorites
                    .Where(uf => uf.Type == "salon")
                    .Select(uf => uf.TargetId)
                    .ToList();

                var specialistIds = favorites
                    .Where(uf => uf.Type == "specialist")
                    .Select(uf => uf.TargetId)
                    .ToList();

                return Results.Ok(new
                {
                    salons = salonIds,
                    specialists = specialistIds
                });
            })
            .WithSummary("Get user favorites")
            .WithDescription("Retrieves the IDs of favorite salons and specialists for the authenticated user.");

            favoritesGroup.MapPost("/toggle", [Authorize] async ([FromBody] ToggleFavoriteDto request, ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken ct) =>
            {
                var nameIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(nameIdClaim) || !Guid.TryParse(nameIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                if (string.IsNullOrWhiteSpace(request.TargetId) || string.IsNullOrWhiteSpace(request.Type))
                {
                    return Results.BadRequest(new { message = "TargetId and Type are required." });
                }

                var targetId = request.TargetId.Trim();
                var type = request.Type.Trim().ToLowerInvariant();

                if (type != "salon" && type != "specialist")
                {
                    return Results.BadRequest(new { message = "Type must be 'salon' or 'specialist'." });
                }

                var existing = await dbContext.UserFavorites
                    .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.TargetId == targetId && uf.Type == type, ct);

                bool isFavorite;
                if (existing != null)
                {
                    dbContext.UserFavorites.Remove(existing);
                    isFavorite = false;
                }
                else
                {
                    var favorite = new UserFavorite(userId, targetId, type);
                    dbContext.UserFavorites.Add(favorite);
                    isFavorite = true;
                }

                await dbContext.SaveChangesAsync(ct);

                return Results.Ok(new
                {
                    targetId = targetId,
                    type = type,
                    isFavorite = isFavorite
                });
            })
            .WithSummary("Toggle favorite item")
            .WithDescription("Toggles favorite status of a salon or specialist for the authenticated user.");

            return app;
        }
    }

    public record ToggleFavoriteDto(string TargetId, string Type);
}
