using System;

namespace movaa_project_back.Domain.Entities
{
    public class OrganizationMembership
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid OrganizationId { get; private set; }
        public string Role { get; private set; } = "MANAGER"; // OWNER, ADMIN, MANAGER, RECEPTIONIST, SPECIALIST
        public string Status { get; private set; } = "ACTIVE"; // ACTIVE, SUSPENDED, PENDING
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        protected OrganizationMembership() { }

        public OrganizationMembership(
            Guid userId,
            Guid organizationId,
            string role = "MANAGER",
            string status = "ACTIVE")
        {
            Id = Guid.NewGuid();
            UserId = userId;
            OrganizationId = organizationId;
            Role = !string.IsNullOrWhiteSpace(role) ? role.Trim().ToUpperInvariant() : "MANAGER";
            Status = !string.IsNullOrWhiteSpace(status) ? status.Trim().ToUpperInvariant() : "ACTIVE";
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateRole(string role)
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                Role = role.Trim().ToUpperInvariant();
                UpdatedAt = DateTime.UtcNow;
            }
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
