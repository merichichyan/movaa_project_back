using System;

namespace movaa_project_back.Domain.Entities
{
    public class Branch
    {
        public Guid Id { get; private set; }
        public Guid OrganizationId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Slug { get; private set; } = string.Empty;
        public string Address { get; private set; } = string.Empty;
        public double? Latitude { get; private set; }
        public double? Longitude { get; private set; }
        public string Phone { get; private set; } = string.Empty;
        public string? Email { get; private set; }
        public string WorkingHours { get; private set; } = "09:00 - 18:00";
        public string CategoriesJson { get; private set; } = "[]";
        public string Status { get; private set; } = "ACTIVE"; // ACTIVE, TEMPORARILY_CLOSED, INACTIVE
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        protected Branch() { }

        public Branch(
            Guid organizationId,
            string name,
            string address,
            string phone,
            string? slug = null,
            double? latitude = null,
            double? longitude = null,
            string? email = null,
            string workingHours = "09:00 - 18:00",
            string status = "ACTIVE",
            string? categoriesJson = null)
        {
            Id = Guid.NewGuid();
            OrganizationId = organizationId;
            Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException("Branch name is required.", nameof(name));
            Address = !string.IsNullOrWhiteSpace(address) ? address.Trim() : throw new ArgumentException("Address is required.", nameof(address));
            Phone = phone.Trim();
            Slug = !string.IsNullOrWhiteSpace(slug) ? slug.Trim().ToLowerInvariant() : Organization.GenerateSlug(Name);
            Latitude = latitude;
            Longitude = longitude;
            Email = email?.Trim().ToLowerInvariant();
            WorkingHours = !string.IsNullOrWhiteSpace(workingHours) ? workingHours.Trim() : "09:00 - 18:00";
            Status = !string.IsNullOrWhiteSpace(status) ? status.Trim().ToUpperInvariant() : "ACTIVE";
            CategoriesJson = !string.IsNullOrWhiteSpace(categoriesJson) ? categoriesJson.Trim() : "[]";
            CreatedAt = DateTime.UtcNow;
        }

        public void Update(
            string name,
            string address,
            string phone,
            double? latitude = null,
            double? longitude = null,
            string? email = null,
            string? workingHours = null,
            string? status = null,
            string? categoriesJson = null)
        {
            if (!string.IsNullOrWhiteSpace(name)) Name = name.Trim();
            if (!string.IsNullOrWhiteSpace(address)) Address = address.Trim();
            Phone = phone.Trim();
            Latitude = latitude ?? Latitude;
            Longitude = longitude ?? Longitude;
            Email = email?.Trim().ToLowerInvariant() ?? Email;
            if (!string.IsNullOrWhiteSpace(workingHours)) WorkingHours = workingHours.Trim();
            if (!string.IsNullOrWhiteSpace(status)) Status = status.Trim().ToUpperInvariant();
            if (categoriesJson != null) CategoriesJson = categoriesJson.Trim();
            UpdatedAt = DateTime.UtcNow;
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
