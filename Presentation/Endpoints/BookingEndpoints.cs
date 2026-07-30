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
    public static class BookingEndpoints
    {
        public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/bookings")
                           .WithTags("Bookings");

            group.MapPost("", [Authorize] async ([FromBody] CreateBookingRequest request, ClaimsPrincipal principal, AppDbContext context, CancellationToken ct) =>
            {
                var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                var emailClaim = principal.FindFirstValue(ClaimTypes.Email) ?? principal.Identity?.Name ?? "user@movaa.com";
                
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var specialist = await context.Specialists.FirstOrDefaultAsync(s => s.Id == request.SpecialistId, ct);
                if (specialist == null)
                {
                    return Results.NotFound(new { message = "Specialist not found" });
                }

                var booking = new Booking(
                    request.SpecialistId,
                    specialist.Name,
                    request.ServiceName,
                    request.Price,
                    request.DurationMinutes,
                    request.BookingDate,
                    request.TimeSlot,
                    userId,
                    emailClaim,
                    request.SalonId,
                    request.SalonName
                );

                context.Bookings.Add(booking);
                await context.SaveChangesAsync(ct);

                return Results.Created($"/api/bookings/{booking.Id}", booking);
            })
            .WithSummary("Create a new booking");

            group.MapGet("", [Authorize] async (ClaimsPrincipal principal, AppDbContext context, CancellationToken ct) =>
            {
                var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var bookings = await context.Bookings
                                            .Where(b => b.UserId == userId)
                                            .OrderByDescending(b => b.BookingDate)
                                            .ToListAsync(ct);

                return Results.Ok(bookings);
            })
            .WithSummary("Get user bookings");

            group.MapGet("/specialist/{specialistId:guid}", async (Guid specialistId, AppDbContext context, CancellationToken ct) =>
            {
                var bookings = await context.Bookings
                                            .Where(b => b.SpecialistId == specialistId)
                                            .ToListAsync(ct);

                return Results.Ok(bookings);
            })
            .WithSummary("Get specialist bookings");

            group.MapDelete("/{id:guid}", [Authorize] async (Guid id, ClaimsPrincipal principal, AppDbContext context, CancellationToken ct) =>
            {
                var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == id, ct);
                if (booking == null)
                {
                    return Results.NotFound(new { message = "Booking not found" });
                }

                if (booking.UserId != userId)
                {
                    return Results.Forbid();
                }

                try
                {
                    var parts = booking.TimeSlot.Split('-');
                    var startPart = parts[0].Trim();
                    var timeParts = startPart.Split(':');
                    var hour = int.Parse(timeParts[0]);
                    var minute = int.Parse(timeParts[1]);

                    var bookingStart = booking.BookingDate.Date.AddHours(hour).AddMinutes(minute);
                    
                    if (bookingStart - DateTime.UtcNow < TimeSpan.FromHours(4))
                    {
                        return Results.BadRequest(new { message = "Cannot cancel a booking less than 4 hours before the scheduled time." });
                      }
                }
                catch (Exception)
                {
                    if (booking.BookingDate.Date <= DateTime.UtcNow.Date)
                    {
                        return Results.BadRequest(new { message = "Cannot cancel a booking scheduled for today or in the past." });
                    }
                }

                context.Bookings.Remove(booking);
                await context.SaveChangesAsync(ct);

                return Results.Ok(new { message = "Booking cancelled successfully" });
            })
            .WithSummary("Cancel a booking");

            return app;
        }
    }

    public record CreateBookingRequest(
        Guid SpecialistId,
        string ServiceName,
        decimal Price,
        int DurationMinutes,
        DateTime BookingDate,
        string TimeSlot,
        Guid? SalonId = null,
        string? SalonName = null
    );
}
