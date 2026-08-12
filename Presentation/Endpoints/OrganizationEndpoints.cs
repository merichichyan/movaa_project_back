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

            // GET /api/organizations/{orgId}/branches
            orgGroup.MapGet("/{orgId:guid}/branches", async (Guid orgId, AppDbContext dbContext, CancellationToken ct) =>
            {
                var branches = await dbContext.Branches
                    .Where(b => b.OrganizationId == orgId)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync(ct);
                return Results.Ok(branches);
            });

            // POST /api/organizations/{orgId}/branches
            orgGroup.MapPost("/{orgId:guid}/branches", async (Guid orgId, [FromBody] CreateBranchDto dto, AppDbContext dbContext, CancellationToken ct) =>
            {
                var org = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == orgId, ct);
                if (org == null) return Results.NotFound(new { message = "Organization not found." });

                var branch = new Branch(
                    organizationId: orgId,
                    name: dto.Name,
                    address: dto.Address,
                    phone: dto.Phone,
                    email: dto.Email,
                    workingHours: dto.WorkingHours ?? "09:00 - 18:00",
                    latitude: dto.Latitude,
                    longitude: dto.Longitude
                );

                dbContext.Branches.Add(branch);
                await dbContext.SaveChangesAsync(ct);
                return Results.Created($"/api/organizations/{orgId}/branches/{branch.Id}", branch);
            });

            // PUT /api/organizations/{orgId}/branches/{branchId}
            orgGroup.MapPut("/{orgId:guid}/branches/{branchId:guid}", async (Guid orgId, Guid branchId, [FromBody] UpdateBranchDto dto, AppDbContext dbContext, CancellationToken ct) =>
            {
                var branch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == branchId && b.OrganizationId == orgId, ct);
                if (branch == null) return Results.NotFound(new { message = "Branch not found." });

                branch.Update(
                    name: dto.Name,
                    address: dto.Address,
                    phone: dto.Phone,
                    latitude: dto.Latitude,
                    longitude: dto.Longitude,
                    email: dto.Email,
                    workingHours: dto.WorkingHours,
                    status: dto.Status
                );

                await dbContext.SaveChangesAsync(ct);
                return Results.Ok(branch);
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
                    longitude: sourceBranch.Longitude
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
