using System;
using System.Collections.Generic;

namespace movaa_project_back.Application.DTOs.Organization
{
    public record CreateOrganizationDto(
        string Name,
        string Phone,
        string? Email,
        string? Website,
        string? LogoUrl,
        string? Description
    );

    public record UpdateOrganizationDto(
        string Name,
        string Phone,
        string? Email,
        string? Website,
        string? LogoUrl,
        string? Description,
        string? Status
    );

    public record CreateBranchDto(
        string Name,
        string Address,
        string Phone,
        string? Email,
        string? WorkingHours,
        double? Latitude,
        double? Longitude,
        List<string>? Categories,
        bool? IsMain,
        string? Instagram,
        string? Facebook,
        List<string>? SpecialistIds
    );

    public record UpdateBranchDto(
        string Name,
        string Address,
        string Phone,
        string? Email,
        string? WorkingHours,
        string? Status,
        double? Latitude,
        double? Longitude,
        List<string>? Categories,
        bool? IsMain,
        string? Instagram,
        string? Facebook,
        List<string>? SpecialistIds
    );

    public record InviteSpecialistDto(
        Guid SpecialistId,
        string? Note
    );

    public record AssignSpecialistBranchDto(
        Guid SpecialistId,
        Guid BranchId
    );
}
