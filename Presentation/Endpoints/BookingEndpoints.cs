using System;
using System.Collections.Generic;
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

                if (request.SpecialistId == Guid.Empty || string.IsNullOrWhiteSpace(request.ServiceName))
                {
                    return Results.BadRequest(new { message = "SpecialistId and ServiceName are required." });
                }

                var effectiveServiceId = !string.IsNullOrWhiteSpace(request.ServiceId) ? request.ServiceId.Trim() : request.ServiceName.Trim();

                // 1. Calculate Time Interval Overlap Range
                var reqDate = request.BookingDate.Date;
                var startTimeStr = request.TimeSlot?.Split('-')[0].Trim();
                TimeSpan startTime;
                if (!TimeSpan.TryParse(startTimeStr, out startTime))
                {
                    startTime = TimeSpan.FromHours(10);
                }
                var reqStart = reqDate.Add(startTime);

                TimeSpan endTime;
                if (request.TimeSlot != null && request.TimeSlot.Contains("-") && TimeSpan.TryParse(request.TimeSlot.Split('-')[1].Trim(), out endTime))
                {
                    // Parsed end time from time slot
                }
                else
                {
                    endTime = startTime.Add(TimeSpan.FromMinutes(request.DurationMinutes > 0 ? request.DurationMinutes : 30));
                }
                var reqEnd = reqDate.Add(endTime);

                // Execute inside a Database Transaction to prevent race conditions during concurrent bookings
                using var transaction = await context.Database.BeginTransactionAsync(ct);
                try
                {
                    // Validate Salon (if provided)
                    if (request.SalonId.HasValue && request.SalonId.Value != Guid.Empty)
                    {
                        var salonExists = await context.Salons.AnyAsync(s => s.Id == request.SalonId.Value, ct);
                        if (!salonExists)
                        {
                            await transaction.RollbackAsync(ct);
                            return Results.BadRequest(new { message = "Invalid SalonId." });
                        }
                    }

                    // 2. Validate Specialist Availability (Only active bookings consume capacity)
                    var existingSpecialistBookings = await context.Bookings
                        .Where(b => b.SpecialistId == request.SpecialistId 
                                 && b.BookingDate.Date == reqDate 
                                 && b.Status != "Cancelled" 
                                 && b.Status != "Rejected")
                        .ToListAsync(ct);

                    foreach (var b in existingSpecialistBookings)
                    {
                        var bStart = b.BookingDate.Date.Add(TimeSpan.TryParse(b.TimeSlot?.Split('-')[0].Trim(), out var st) ? st : TimeSpan.FromHours(10));
                        var bEnd = b.BookingDate.Date.Add(b.TimeSlot != null && b.TimeSlot.Contains("-") && TimeSpan.TryParse(b.TimeSlot.Split('-')[1].Trim(), out var et) ? et : st.Add(TimeSpan.FromMinutes(b.DurationMinutes > 0 ? b.DurationMinutes : 30)));

                        // Proper time interval overlap logic: reqStart < bEnd && bStart < reqEnd
                        if (reqStart < bEnd && bStart < reqEnd)
                        {
                            await transaction.RollbackAsync(ct);
                            return Results.BadRequest(new { message = "The specialist is already booked at the requested time slot." });
                        }
                    }

                    // 3. Validate Resource Availability if SalonId is provided
                    if (request.SalonId.HasValue && request.SalonId.Value != Guid.Empty)
                    {
                        var salonId = request.SalonId.Value;
                        var sIdLower = effectiveServiceId.ToLower();
                        var sNameLower = request.ServiceName.Trim().ToLower();

                        // Query required resources using ServiceId (and fallback ServiceName)
                        var requiredResources = await context.ServiceResources
                            .Include(sr => sr.Resource)
                            .Where(sr => sr.SalonId == salonId && (sr.ServiceId.ToLower() == sIdLower || (sr.ServiceName != null && sr.ServiceName.ToLower() == sNameLower)))
                            .ToListAsync(ct);

                        if (requiredResources.Count > 0)
                        {
                            // Fetch active bookings in the same salon on the same date
                            var existingSalonBookings = await context.Bookings
                                .Where(b => b.SalonId == salonId 
                                         && b.BookingDate.Date == reqDate 
                                         && b.Status != "Cancelled" 
                                         && b.Status != "Rejected")
                                .ToListAsync(ct);

                            // Find overlapping active bookings using proper interval overlap logic
                            var overlappingBookings = new List<Booking>();
                            foreach (var b in existingSalonBookings)
                            {
                                var bStart = b.BookingDate.Date.Add(TimeSpan.TryParse(b.TimeSlot?.Split('-')[0].Trim(), out var st) ? st : TimeSpan.FromHours(10));
                                var bEnd = b.BookingDate.Date.Add(b.TimeSlot != null && b.TimeSlot.Contains("-") && TimeSpan.TryParse(b.TimeSlot.Split('-')[1].Trim(), out var et) ? et : st.Add(TimeSpan.FromMinutes(b.DurationMinutes > 0 ? b.DurationMinutes : 30)));

                                if (reqStart < bEnd && bStart < reqEnd)
                                {
                                    overlappingBookings.Add(b);
                                }
                            }

                            // Calculate resource consumption using RequiredQuantity
                            foreach (var reqRes in requiredResources)
                            {
                                if (reqRes.Resource == null || !reqRes.Resource.IsActive) continue;

                                int consumedQuantity = 0;
                                foreach (var ob in overlappingBookings)
                                {
                                    var obServiceIdLower = (!string.IsNullOrWhiteSpace(ob.ServiceId) ? ob.ServiceId : ob.ServiceName).Trim().ToLower();
                                    var obServiceNameLower = ob.ServiceName.Trim().ToLower();

                                    var obReqRes = await context.ServiceResources
                                        .FirstOrDefaultAsync(sr => sr.SalonId == salonId 
                                                                && (sr.ServiceId.ToLower() == obServiceIdLower || (sr.ServiceName != null && sr.ServiceName.ToLower() == obServiceNameLower))
                                                                && sr.ResourceId == reqRes.ResourceId, ct);
                                    if (obReqRes != null)
                                    {
                                        consumedQuantity += obReqRes.RequiredQuantity;
                                    }
                                }

                                if (consumedQuantity + reqRes.RequiredQuantity > reqRes.Resource.Quantity)
                                {
                                    await transaction.RollbackAsync(ct);
                                    return Results.BadRequest(new { message = $"No available {reqRes.Resource.Name}. The resource is fully booked at the requested time slot." });
                                }
                            }
                        }
                    }

                    var specialist = await context.Specialists.FirstOrDefaultAsync(s => s.Id == request.SpecialistId, ct);
                    var specName = specialist?.Name ?? request.SpecialistName ?? "Specialist";

                    var booking = new Booking(
                        request.SpecialistId,
                        specName,
                        request.ServiceName,
                        request.Price,
                        request.DurationMinutes,
                        request.BookingDate,
                        request.TimeSlot,
                        userId,
                        emailClaim,
                        request.SalonId,
                        request.SalonName,
                        serviceId: effectiveServiceId,
                        status: "Confirmed"
                    );

                    context.Bookings.Add(booking);
                    await context.SaveChangesAsync(ct);

                    await transaction.CommitAsync(ct);
                    return Results.Created($"/api/bookings/{booking.Id}", booking);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    return Results.Problem(detail: $"Error processing booking: {ex.Message}", statusCode: 500);
                }
            })
            .WithSummary("Create a new booking with transaction and resource validation");

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

                await PopulateUserDetailsAsync(context, bookings, ct);
                return Results.Ok(bookings);
            })
            .WithSummary("Get user bookings");

            group.MapGet("/specialist/{specialistId:guid}", async (Guid specialistId, AppDbContext context, CancellationToken ct) =>
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-90);
                var bookings = await context.Bookings
                                            .Where(b => b.SpecialistId == specialistId && b.BookingDate >= cutoffDate && b.Status != "Cancelled" && b.Status != "Rejected")
                                            .OrderByDescending(b => b.BookingDate)
                                            .ToListAsync(ct);

                await PopulateUserDetailsAsync(context, bookings, ct);
                return Results.Ok(bookings);
            })
            .WithSummary("Get specialist bookings");

            group.MapGet("/salon/{salonId:guid}", async (Guid salonId, AppDbContext context, CancellationToken ct) =>
            {
                var bookings = await context.Bookings
                                            .Where(b => b.SalonId == salonId && b.Status != "Cancelled" && b.Status != "Rejected")
                                            .OrderByDescending(b => b.BookingDate)
                                            .ToListAsync(ct);

                await PopulateUserDetailsAsync(context, bookings, ct);
                return Results.Ok(bookings);
            })
            .WithSummary("Get salon bookings");

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

                booking.SetStatus("Cancelled");
                await context.SaveChangesAsync(ct);

                return Results.Ok(new { message = "Booking cancelled successfully" });
            })
            .WithSummary("Cancel a booking");

            return app;
        }

        static async Task PopulateUserDetailsAsync(AppDbContext context, List<Booking> bookings, CancellationToken ct)
        {
            if (bookings.Count == 0) return;
            var userIds = bookings.Select(b => b.UserId).Distinct().ToList();
            var users = await context.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);
            foreach (var b in bookings)
            {
                if (users.TryGetValue(b.UserId, out var u))
                {
                    b.UserName = u.FullName;
                    b.UserPhone = u.Phone;
                }
            }
        }
    }

    public record CreateBookingRequest(
        Guid SpecialistId,
        string ServiceName,
        decimal Price,
        int DurationMinutes,
        DateTime BookingDate,
        string TimeSlot,
        string? ServiceId = null,
        string? SpecialistName = null,
        Guid? SalonId = null,
        string? SalonName = null
    );
}
