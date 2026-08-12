using System;

namespace movaa_project_back.Domain.Entities
{
    public class SpecialistBranch
    {
        public Guid Id { get; private set; }
        public Guid SpecialistId { get; private set; }
        public Guid BranchId { get; private set; }
        public Guid OrganizationId { get; private set; }
        public string Status { get; private set; } = "ACTIVE"; // ACTIVE, INACTIVE
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        protected SpecialistBranch() { }

        public SpecialistBranch(
            Guid specialistId,
            Guid branchId,
            Guid organizationId,
            string status = "ACTIVE")
        {
            Id = Guid.NewGuid();
            SpecialistId = specialistId;
            BranchId = branchId;
            OrganizationId = organizationId;
            Status = !string.IsNullOrWhiteSpace(status) ? status.Trim().ToUpperInvariant() : "ACTIVE";
            CreatedAt = DateTime.UtcNow;
        }

        public void SetStatus(string status)
        {
            if (!string.IsNullOrWhiteSpace(status))
            {
                Status = status.Trim().ToUpperInvariant();
                UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
