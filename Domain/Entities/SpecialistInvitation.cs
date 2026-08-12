using System;

namespace movaa_project_back.Domain.Entities
{
    public class SpecialistInvitation
    {
        public Guid Id { get; private set; }
        public Guid OrganizationId { get; private set; }
        public Guid SpecialistId { get; private set; }
        public Guid? InvitedByUserId { get; private set; }
        public string Status { get; private set; } = "PENDING"; // PENDING, ACCEPTED, DECLINED, EXPIRED, CANCELLED
        public string? Note { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        protected SpecialistInvitation() { }

        public SpecialistInvitation(
            Guid organizationId,
            Guid specialistId,
            Guid? invitedByUserId = null,
            string? note = null)
        {
            Id = Guid.NewGuid();
            OrganizationId = organizationId;
            SpecialistId = specialistId;
            InvitedByUserId = invitedByUserId;
            Note = note?.Trim();
            Status = "PENDING";
            CreatedAt = DateTime.UtcNow;
        }

        public void Accept()
        {
            Status = "ACCEPTED";
            UpdatedAt = DateTime.UtcNow;
        }

        public void Decline()
        {
            Status = "DECLINED";
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            Status = "CANCELLED";
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
