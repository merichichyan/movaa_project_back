using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movaa_project_back.Application.DTOs.Admin;
using movaa_project_back.Data;
using movaa_project_back.Domain.Entities;

namespace movaa_project_back.Presentation.Endpoints;

public static class AdminOfferEndpoints
{
    public static void MapOfferEndpoints(this IEndpointRouteBuilder app)
    {
        var apiGroup = app.MapGroup("/api");

        // Function to get active or all offers
        async Task<IResult> GetOffersHandler(AppDbContext dbContext, CancellationToken ct, bool activeOnly = false, Guid? specialistId = null)
        {
            try
            {
                var query = dbContext.Offers.AsQueryable();
                if (activeOnly)
                {
                    query = query.Where(o => o.IsActive);
                }
                if (specialistId.HasValue)
                {
                    query = query.Where(o => o.SpecialistId == specialistId.Value);
                }

                var offers = await query
                    .OrderBy(o => o.OrderIndex)
                    .ThenByDescending(o => o.CreatedAt)
                    .ToListAsync(ct);
                return Results.Ok(offers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching offers: {ex}");
                return Results.Ok(new List<Offer>());
            }
        }

        async Task<IResult> CreateOfferHandler(CreateOfferDto dto, AppDbContext dbContext, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return Results.BadRequest(new { message = "Title is required." });
            }

            var offer = new Offer(
                title: dto.Title,
                titleHy: dto.TitleHy,
                titleEn: dto.TitleEn,
                titleRu: dto.TitleRu,
                subtitle: dto.Subtitle,
                subtitleHy: dto.SubtitleHy,
                subtitleEn: dto.SubtitleEn,
                subtitleRu: dto.SubtitleRu,
                badgeText: dto.BadgeText,
                badgeTextHy: dto.BadgeTextHy,
                badgeTextEn: dto.BadgeTextEn,
                badgeTextRu: dto.BadgeTextRu,
                discountPercent: dto.DiscountPercent,
                salonId: dto.SalonId,
                salonName: dto.SalonName,
                specialistId: dto.SpecialistId,
                specialistName: dto.SpecialistName,
                imageUrl: dto.ImageUrl,
                validUntil: dto.ValidUntil,
                orderIndex: dto.OrderIndex,
                isActive: dto.IsActive
            );

            try
            {
                dbContext.Offers.Add(offer);
                await dbContext.SaveChangesAsync(ct);
                return Results.Ok(offer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating offer: {ex}");
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        }

        async Task<IResult> UpdateOfferHandler(Guid id, UpdateOfferDto dto, AppDbContext dbContext, CancellationToken ct)
        {
            var offer = await dbContext.Offers.FirstOrDefaultAsync(o => o.Id == id, ct);
            if (offer == null) return Results.NotFound(new { message = "Offer not found." });

            offer.Update(
                title: dto.Title,
                titleHy: dto.TitleHy,
                titleEn: dto.TitleEn,
                titleRu: dto.TitleRu,
                subtitle: dto.Subtitle,
                subtitleHy: dto.SubtitleHy,
                subtitleEn: dto.SubtitleEn,
                subtitleRu: dto.SubtitleRu,
                badgeText: dto.BadgeText,
                badgeTextHy: dto.BadgeTextHy,
                badgeTextEn: dto.BadgeTextEn,
                badgeTextRu: dto.BadgeTextRu,
                discountPercent: dto.DiscountPercent,
                salonId: dto.SalonId,
                salonName: dto.SalonName,
                specialistId: dto.SpecialistId,
                specialistName: dto.SpecialistName,
                imageUrl: dto.ImageUrl,
                validUntil: dto.ValidUntil,
                orderIndex: dto.OrderIndex,
                isActive: dto.IsActive
            );

            try
            {
                await dbContext.SaveChangesAsync(ct);
                return Results.Ok(offer);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        }

        async Task<IResult> ReorderOffersHandler(ReorderOfferDto dto, AppDbContext dbContext, CancellationToken ct)
        {
            if (dto.OfferIds == null || dto.OfferIds.Count == 0) return Results.Ok();

            for (int i = 0; i < dto.OfferIds.Count; i++)
            {
                var id = dto.OfferIds[i];
                var offer = await dbContext.Offers.FirstOrDefaultAsync(o => o.Id == id, ct);
                if (offer != null)
                {
                    offer.SetOrderIndex(i);
                }
            }

            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Offers reordered successfully." });
        }

        async Task<IResult> DeleteOfferHandler(Guid id, AppDbContext dbContext, CancellationToken ct)
        {
            var offer = await dbContext.Offers.FirstOrDefaultAsync(o => o.Id == id, ct);
            if (offer == null) return Results.NotFound(new { message = "Offer not found." });

            dbContext.Offers.Remove(offer);
            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Offer deleted successfully." });
        }

        async Task<IResult> ToggleOfferHandler(Guid id, BlockToggleDto dto, AppDbContext dbContext, CancellationToken ct)
        {
            var offer = await dbContext.Offers.FirstOrDefaultAsync(o => o.Id == id, ct);
            if (offer == null) return Results.NotFound(new { message = "Offer not found." });

            offer.ToggleActive(!dto.IsBlocked);
            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(offer);
        }

        // Register routes under /api/offers (Public endpoint defaults to activeOnly = true)
        apiGroup.MapGet("/offers", async (AppDbContext dbContext, CancellationToken ct, [FromQuery] bool? activeOnly, [FromQuery] Guid? specialistId) => await GetOffersHandler(dbContext, ct, activeOnly: activeOnly ?? true, specialistId: specialistId));
        apiGroup.MapPost("/offers", async ([FromBody] CreateOfferDto dto, AppDbContext dbContext, CancellationToken ct) => await CreateOfferHandler(dto, dbContext, ct));
        apiGroup.MapPost("/offers/reorder", async ([FromBody] ReorderOfferDto dto, AppDbContext dbContext, CancellationToken ct) => await ReorderOffersHandler(dto, dbContext, ct));
        apiGroup.MapPut("/offers/{id:guid}", async (Guid id, [FromBody] UpdateOfferDto dto, AppDbContext dbContext, CancellationToken ct) => await UpdateOfferHandler(id, dto, dbContext, ct));
        apiGroup.MapDelete("/offers/{id:guid}", async (Guid id, AppDbContext dbContext, CancellationToken ct) => await DeleteOfferHandler(id, dbContext, ct));
        apiGroup.MapPost("/offers/{id:guid}/toggle", async (Guid id, [FromBody] BlockToggleDto dto, AppDbContext dbContext, CancellationToken ct) => await ToggleOfferHandler(id, dto, dbContext, ct));

        // Register routes under /api/admin/offers
        var adminGroup = apiGroup.MapGroup("/admin");
        adminGroup.MapGet("/offers", async (AppDbContext dbContext, CancellationToken ct, [FromQuery] Guid? specialistId) => await GetOffersHandler(dbContext, ct, activeOnly: false, specialistId: specialistId));
        adminGroup.MapPost("/offers", async ([FromBody] CreateOfferDto dto, AppDbContext dbContext, CancellationToken ct) => await CreateOfferHandler(dto, dbContext, ct));
        adminGroup.MapPost("/offers/reorder", async ([FromBody] ReorderOfferDto dto, AppDbContext dbContext, CancellationToken ct) => await ReorderOffersHandler(dto, dbContext, ct));
        adminGroup.MapPut("/offers/{id:guid}", async (Guid id, [FromBody] UpdateOfferDto dto, AppDbContext dbContext, CancellationToken ct) => await UpdateOfferHandler(id, dto, dbContext, ct));
        adminGroup.MapDelete("/offers/{id:guid}", async (Guid id, AppDbContext dbContext, CancellationToken ct) => await DeleteOfferHandler(id, dbContext, ct));
        adminGroup.MapPost("/offers/{id:guid}/toggle", async (Guid id, [FromBody] BlockToggleDto dto, AppDbContext dbContext, CancellationToken ct) => await ToggleOfferHandler(id, dto, dbContext, ct));
    }
}
