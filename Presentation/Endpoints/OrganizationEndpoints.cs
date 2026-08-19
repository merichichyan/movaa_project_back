using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using movaa_project_back.Application.DTOs.Organization;
using movaa_project_back.Data;
using movaa_project_back.Domain.Entities;

namespace movaa_project_back.Presentation.Endpoints
{
    public static class OrganizationEndpoints
    {
        public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
        {
            var orgGroup = app.MapGroup("/api/organizations").WithTags("Organizations");

            // GET /api/organizations
            orgGroup.MapGet("/", async (AppDbContext dbContext, CancellationToken ct) =>
            {
                var orgs = await dbContext.Organizations
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync(ct);
                return Results.Ok(orgs);
            });

            // GET /api/organizations/{id}
            orgGroup.MapGet("/{id:guid}", async (Guid id, AppDbContext dbContext, CancellationToken ct) =>
            {
                var org = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);
                if (org == null) return Results.NotFound(new { message = "Organization not found." });

                var branches = await dbContext.Branches.Where(b => b.OrganizationId == id).ToListAsync(ct);
                var memberships = await dbContext.OrganizationMemberships.Where(m => m.OrganizationId == id).ToListAsync(ct);

                return Results.Ok(new { organization = org, branches, membershipCount = memberships.Count });
            });

            // POST /api/organizations
            orgGroup.MapPost("/", async ([FromBody] CreateOrganizationDto dto, AppDbContext dbContext, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return Results.BadRequest(new { message = "Organization name is required." });
                }

                var org = new Organization(
                    name: dto.Name,
                    phone: dto.Phone,
                    email: dto.Email,
                    website: dto.Website,
                    logoUrl: dto.LogoUrl,
                    description: dto.Description
                );

                dbContext.Organizations.Add(org);

                // Auto-create initial default Branch (Headquarters)
                var defaultBranch = new Branch(
                    organizationId: org.Id,
                    name: $"{org.Name} - Main Branch",
                    address: "Main Address",
                    phone: dto.Phone,
                    email: dto.Email
                );
                dbContext.Branches.Add(defaultBranch);

                await dbContext.SaveChangesAsync(ct);
                return Results.Created($"/api/organizations/{org.Id}", new { organization = org, defaultBranch });
            });

            // PUT /api/organizations/{id}
            orgGroup.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateOrganizationDto dto, AppDbContext dbContext, CancellationToken ct) =>
            {
                var org = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);
                if (org == null) return Results.NotFound(new { message = "Organization not found." });

                org.Update(
                    name: dto.Name,
                    phone: dto.Phone,
                    email: dto.Email,
                    website: dto.Website,
                    logoUrl: dto.LogoUrl,
                    description: dto.Description,
                    status: dto.Status
                );

                await dbContext.SaveChangesAsync(ct);
                return Results.Ok(org);
            });

            // ---------------- BRANCHES ----------------

            // Helper to map Branch entity to response DTO with specialistIds
            static async Task<object> MapBranchResponseAsync(Branch b, AppDbContext dbContext, CancellationToken ct)
            {
                List<string> specIds = new();
                try
                {
                    specIds = await dbContext.SpecialistBranches
                        .Where(sb => sb.BranchId == b.Id && sb.Status == "ACTIVE")
                        .Select(sb => sb.SpecialistId.ToString())
                        .ToListAsync(ct);
                }
                catch { }

                bool isMain = false;
                string? insta = null;
                string? fb = null;

                try { isMain = b.IsMain; } catch { }
                try { insta = b.Instagram; } catch { }
                try { fb = b.Facebook; } catch { }

                return new
                {
                    id = b.Id.ToString(),
                    organizationId = b.OrganizationId.ToString(),
                    name = b.Name,
                    address = b.Address,
                    phone = b.Phone,
                    email = b.Email,
                    workingHours = b.WorkingHours,
                    status = b.Status,
                    isActive = b.Status == "ACTIVE",
                    isMain = isMain,
                    instagram = insta,
                    facebook = fb,
                    latitude = b.Latitude,
                    longitude = b.Longitude,
                    specialistIds = specIds,
                    createdAt = b.CreatedAt,
                    updatedAt = b.UpdatedAt
                };
            }

            // GET /api/organizations/{orgId}/branches & /api/salons/{orgId}/branches
            async Task<IResult> GetBranchesHandler(Guid orgId, AppDbContext dbContext, CancellationToken ct)
            {
                try
                {
                    var branches = await dbContext.Branches
                        .Where(b => b.OrganizationId == orgId)
                        .OrderByDescending(b => b.IsMain)
                        .ThenBy(b => b.CreatedAt)
                        .ToListAsync(ct);

                    var resultList = new List<object>();
                    foreach (var b in branches)
                    {
                        resultList.Add(await MapBranchResponseAsync(b, dbContext, ct));
                    }
                    return Results.Ok(resultList);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GetBranches Main Query Error: {ex.Message}");
                    try
                    {
                        var branches = await dbContext.Branches
                            .Where(b => b.OrganizationId == orgId)
                            .ToListAsync(ct);

                        var fallbackList = branches.Select(b => new
                        {
                            id = b.Id.ToString(),
                            organizationId = b.OrganizationId.ToString(),
                            name = b.Name,
                            address = b.Address,
                            phone = b.Phone,
                            email = b.Email,
                            workingHours = b.WorkingHours,
                            status = b.Status,
                            isActive = b.Status == "ACTIVE",
                            isMain = false,
                            instagram = (string?)null,
                            facebook = (string?)null,
                            specialistIds = new List<string>()
                        }).ToList();

                        return Results.Ok(fallbackList);
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"GetBranches Fallback Error: {ex2.Message}");
                        return Results.Ok(new List<object>());
                    }
                }
            }

            orgGroup.MapGet("/{orgId:guid}/branches", GetBranchesHandler);
            app.MapGet("/api/salons/{orgId:guid}/branches", GetBranchesHandler);

            // POST /api/organizations/{orgId}/branches & /api/salons/{orgId}/branches
            async Task<IResult> CreateBranchHandler(Guid orgId, [FromBody] CreateBranchDto dto, AppDbContext dbContext, CancellationToken ct)
            {
                var isMain = dto.IsMain ?? false;
                if (isMain)
                {
                    var existingMains = await dbContext.Branches.Where(b => b.OrganizationId == orgId && b.IsMain).ToListAsync(ct);
                    foreach (var m in existingMains)
                    {
                        m.SetIsMain(false);
                    }
                }

                var branch = new Branch(
                    organizationId: orgId,
                    name: dto.Name,
                    address: dto.Address,
                    phone: dto.Phone,
                    email: dto.Email,
                    workingHours: dto.WorkingHours ?? "09:00 - 18:00",
                    latitude: dto.Latitude,
                    longitude: dto.Longitude,
                    isMain: isMain,
                    instagram: dto.Instagram,
                    facebook: dto.Facebook
                );

                dbContext.Branches.Add(branch);

                if (dto.SpecialistIds != null)
                {
                    foreach (var sIdStr in dto.SpecialistIds)
                    {
                        if (Guid.TryParse(sIdStr, out var sGuid))
                        {
                            var link = new SpecialistBranch(sGuid, branch.Id, orgId);
                            dbContext.SpecialistBranches.Add(link);
                        }
                    }
                }

                await dbContext.SaveChangesAsync(ct);
                var res = await MapBranchResponseAsync(branch, dbContext, ct);
                return Results.Created($"/api/organizations/{orgId}/branches/{branch.Id}", res);
            }

            orgGroup.MapPost("/{orgId:guid}/branches", CreateBranchHandler);
            app.MapPost("/api/salons/{orgId:guid}/branches", CreateBranchHandler);

            // PUT /api/organizations/{orgId}/branches/{branchId} & /api/salons/{orgId}/branches/{branchId}
            async Task<IResult> UpdateBranchHandler(Guid orgId, Guid branchId, [FromBody] UpdateBranchDto dto, AppDbContext dbContext, CancellationToken ct)
            {
                var branch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == branchId && b.OrganizationId == orgId, ct);
                if (branch == null)
                {
                    branch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == branchId, ct);
                }
                if (branch == null) return Results.NotFound(new { message = "Branch not found." });

                if (dto.IsMain == true)
                {
                    var existingMains = await dbContext.Branches.Where(b => b.OrganizationId == branch.OrganizationId && b.Id != branch.Id && b.IsMain).ToListAsync(ct);
                    foreach (var m in existingMains)
                    {
                        m.SetIsMain(false);
                    }
                }

                branch.Update(
                    name: dto.Name,
                    address: dto.Address,
                    phone: dto.Phone,
                    latitude: dto.Latitude,
                    longitude: dto.Longitude,
                    email: dto.Email,
                    workingHours: dto.WorkingHours,
                    status: dto.Status,
                    isMain: dto.IsMain,
                    instagram: dto.Instagram,
                    facebook: dto.Facebook
                );

                if (dto.SpecialistIds != null)
                {
                    var existingLinks = await dbContext.SpecialistBranches.Where(sb => sb.BranchId == branchId).ToListAsync(ct);
                    dbContext.SpecialistBranches.RemoveRange(existingLinks);

                    foreach (var sIdStr in dto.SpecialistIds)
                    {
                        if (Guid.TryParse(sIdStr, out var sGuid))
                        {
                            var link = new SpecialistBranch(sGuid, branch.Id, branch.OrganizationId);
                            dbContext.SpecialistBranches.Add(link);
                        }
                    }
                }

                await dbContext.SaveChangesAsync(ct);
                var res = await MapBranchResponseAsync(branch, dbContext, ct);
                return Results.Ok(res);
            }

            orgGroup.MapPut("/{orgId:guid}/branches/{branchId:guid}", UpdateBranchHandler);
            app.MapPut("/api/salons/{orgId:guid}/branches/{branchId:guid}", UpdateBranchHandler);
            app.MapPut("/api/branches/{branchId:guid}", async (Guid branchId, [FromBody] UpdateBranchDto dto, AppDbContext dbContext, CancellationToken ct) =>
            {
                var branch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == branchId, ct);
                if (branch == null) return Results.NotFound(new { message = "Branch not found." });
                return await UpdateBranchHandler(branch.OrganizationId, branchId, dto, dbContext, ct);
            });

            // DELETE /api/organizations/{orgId}/branches/{branchId} & /api/salons/{orgId}/branches/{branchId} & /api/branches/{branchId}
            async Task<IResult> DeleteBranchHandler(Guid orgId, Guid branchId, AppDbContext dbContext, CancellationToken ct)
            {
                var branch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == branchId, ct);
                if (branch == null) return Results.NotFound(new { message = "Branch not found." });

                if (branch.IsMain)
                {
                    return Results.BadRequest(new { message = "Cannot delete main branch." });
                }

                var specLinks = await dbContext.SpecialistBranches.Where(sb => sb.BranchId == branchId).ToListAsync(ct);
                dbContext.SpecialistBranches.RemoveRange(specLinks);

                dbContext.Branches.Remove(branch);
                await dbContext.SaveChangesAsync(ct);

                return Results.Ok(new { message = "Branch deleted successfully." });
            }

            orgGroup.MapDelete("/{orgId:guid}/branches/{branchId:guid}", DeleteBranchHandler);
            app.MapDelete("/api/salons/{orgId:guid}/branches/{branchId:guid}", DeleteBranchHandler);
            app.MapDelete("/api/branches/{branchId:guid}", async (Guid branchId, AppDbContext dbContext, CancellationToken ct) =>
            {
                var branch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == branchId, ct);
                if (branch == null) return Results.NotFound(new { message = "Branch not found." });
                return await DeleteBranchHandler(branch.OrganizationId, branchId, dbContext, ct);
            });

            // POST /api/organizations/{orgId}/branches/{branchId}/duplicate
            orgGroup.MapPost("/{orgId:guid}/branches/{branchId:guid}/duplicate", async (Guid orgId, Guid branchId, AppDbContext dbContext, CancellationToken ct) =>
            {
                var sourceBranch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == branchId && b.OrganizationId == orgId, ct);
                if (sourceBranch == null) return Results.NotFound(new { message = "Source branch not found." });

                var newBranch = new Branch(
                    organizationId: orgId,
                    name: $"{sourceBranch.Name} (Copy)",
                    address: sourceBranch.Address,
                    phone: sourceBranch.Phone,
                    email: sourceBranch.Email,
                    workingHours: sourceBranch.WorkingHours,
                    latitude: sourceBranch.Latitude,
                    longitude: sourceBranch.Longitude,
                    instagram: sourceBranch.Instagram,
                    facebook: sourceBranch.Facebook
                );
                dbContext.Branches.Add(newBranch);

                // Copy resources from source branch
                var sourceResources = await dbContext.SalonResources.Where(r => r.SalonId == branchId).ToListAsync(ct);
                foreach (var res in sourceResources)
                {
                    var newRes = new SalonResource(newBranch.Id, res.Name, res.Quantity, res.Description, res.IsActive);
                    dbContext.SalonResources.Add(newRes);
                }

                await dbContext.SaveChangesAsync(ct);
                return Results.Ok(newBranch);
            });

            // ---------------- SPECIALISTS & INVITATIONS ----------------

            // GET /api/organizations/{orgId}/specialists
            orgGroup.MapGet("/{orgId:guid}/specialists", async (Guid orgId, AppDbContext dbContext, CancellationToken ct) =>
            {
                var specBranches = await dbContext.SpecialistBranches
                    .Where(sb => sb.OrganizationId == orgId && sb.Status == "ACTIVE")
                    .ToListAsync(ct);

                var specIds = specBranches.Select(sb => sb.SpecialistId).Distinct().ToList();
                var specialists = await dbContext.Specialists
                    .Where(s => specIds.Contains(s.Id))
                    .ToListAsync(ct);

                var result = specialists.Select(s => new
                {
                    specialist = s,
                    assignedBranches = specBranches.Where(sb => sb.SpecialistId == s.Id).Select(sb => sb.BranchId).ToList()
                });

                return Results.Ok(result);
            });

            // POST /api/organizations/{orgId}/invitations
            orgGroup.MapPost("/{orgId:guid}/invitations", async (Guid orgId, [FromBody] InviteSpecialistDto dto, AppDbContext dbContext, CancellationToken ct) =>
            {
                var org = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == orgId, ct);
                if (org == null) return Results.NotFound(new { message = "Organization not found." });

                var specialist = await dbContext.Specialists.FirstOrDefaultAsync(s => s.Id == dto.SpecialistId, ct);
                if (specialist == null)
                {
                    return Results.NotFound(new { message = "Specialist not found. New specialist accounts are created by the platform administrator." });
                }

                // Check existing pending or active invitation
                var existing = await dbContext.SpecialistInvitations
                    .FirstOrDefaultAsync(i => i.OrganizationId == orgId && i.SpecialistId == dto.SpecialistId && i.Status == "PENDING", ct);

                if (existing != null)
                {
                    return Results.Conflict(new { message = "An active invitation has already been sent to this specialist." });
                }

                var invitation = new SpecialistInvitation(orgId, dto.SpecialistId, note: dto.Note);
                dbContext.SpecialistInvitations.Add(invitation);

                // Auto-link to org for demonstration/immediate access
                invitation.Accept();

                var defaultBranch = await dbContext.Branches.FirstOrDefaultAsync(b => b.OrganizationId == orgId, ct);
                if (defaultBranch != null)
                {
                    var specBranch = new SpecialistBranch(dto.SpecialistId, defaultBranch.Id, orgId);
                    dbContext.SpecialistBranches.Add(specBranch);
                }

                await dbContext.SaveChangesAsync(ct);
                return Results.Ok(new { invitation, message = "Specialist invited and linked successfully." });
            });

            // POST /api/organizations/{orgId}/branches/{branchId}/specialists
            orgGroup.MapPost("/{orgId:guid}/branches/{branchId:guid}/specialists", async (Guid orgId, Guid branchId, [FromBody] AssignSpecialistBranchDto dto, AppDbContext dbContext, CancellationToken ct) =>
            {
                var existing = await dbContext.SpecialistBranches
                    .FirstOrDefaultAsync(sb => sb.SpecialistId == dto.SpecialistId && sb.BranchId == branchId, ct);

                if (existing != null)
                {
                    existing.SetStatus("ACTIVE");
                }
                else
                {
                    var link = new SpecialistBranch(dto.SpecialistId, branchId, orgId);
                    dbContext.SpecialistBranches.Add(link);
                }

                await dbContext.SaveChangesAsync(ct);
                return Results.Ok(new { message = "Specialist assigned to branch successfully." });
            });

            // DELETE /api/organizations/{orgId}/branches/{branchId}/specialists/{specialistId}
            orgGroup.MapDelete("/{orgId:guid}/branches/{branchId:guid}/specialists/{specialistId:guid}", async (Guid orgId, Guid branchId, Guid specialistId, AppDbContext dbContext, CancellationToken ct) =>
            {
                var link = await dbContext.SpecialistBranches
                    .FirstOrDefaultAsync(sb => sb.SpecialistId == specialistId && sb.BranchId == branchId, ct);

                if (link != null)
                {
                    dbContext.SpecialistBranches.Remove(link);
                    await dbContext.SaveChangesAsync(ct);
                }

                return Results.Ok(new { message = "Specialist removed from branch successfully." });
            });

            return app;
        }
    }
}
