using System;

namespace movaa_project_back.Domain.Entities
{
    public class Organization
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Slug { get; private set; } = string.Empty;
        public string? LogoUrl { get; private set; }
        public string? Description { get; private set; }
        public string Phone { get; private set; } = string.Empty;
        public string? Email { get; private set; }
        public string? Website { get; private set; }
        public string Status { get; private set; } = "ACTIVE";
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        protected Organization() { }

        public Organization(
            string name,
            string? slug = null,
            string phone = "",
            string? email = null,
            string? website = null,
            string? logoUrl = null,
            string? description = null,
            string status = "ACTIVE")
        {
            Id = Guid.NewGuid();
            Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException("Organization name is required.", nameof(name));
            Slug = !string.IsNullOrWhiteSpace(slug) ? slug.Trim().ToLowerInvariant() : GenerateSlug(Name);
            Phone = phone.Trim();
            Email = email?.Trim().ToLowerInvariant();
            Website = website?.Trim();
            LogoUrl = logoUrl?.Trim();
            Description = description?.Trim();
            Status = !string.IsNullOrWhiteSpace(status) ? status.Trim().ToUpperInvariant() : "ACTIVE";
            CreatedAt = DateTime.UtcNow;
        }

        public void Update(
            string name,
            string phone,
            string? email = null,
            string? website = null,
            string? logoUrl = null,
            string? description = null,
            string? status = null)
        {
            if (!string.IsNullOrWhiteSpace(name)) Name = name.Trim();
            Phone = phone.Trim();
            Email = email?.Trim().ToLowerInvariant() ?? Email;
            Website = website?.Trim() ?? Website;
            LogoUrl = logoUrl?.Trim() ?? LogoUrl;
            Description = description?.Trim() ?? Description;
            if (!string.IsNullOrWhiteSpace(status)) Status = status.Trim().ToUpperInvariant();
            UpdatedAt = DateTime.UtcNow;
        }

        public static string GenerateSlug(string name)
        {
            var clean = System.Text.RegularExpressions.Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
            clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\s+", "-").Trim('-');
            return string.IsNullOrWhiteSpace(clean) ? Guid.NewGuid().ToString("n")[..8] : clean;
        }
    }
}
