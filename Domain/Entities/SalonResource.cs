using System;

namespace movaa_project_back.Domain.Entities
{
    public class SalonResource
    {
        public Guid Id { get; private set; }
        public Guid SalonId { get; private set; }
        public string Name { get; private set; }
        public string NameHy { get; private set; } = string.Empty;
        public string NameEn { get; private set; } = string.Empty;
        public string NameRu { get; private set; } = string.Empty;
        public int Quantity { get; private set; }
        public string? Description { get; private set; }
        public string? DescriptionHy { get; private set; }
        public string? DescriptionEn { get; private set; }
        public string? DescriptionRu { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private SalonResource() { }

        public SalonResource(
            Guid salonId,
            string name,
            int quantity,
            string? description = null,
            bool isActive = true,
            string? nameHy = null,
            string? nameEn = null,
            string? nameRu = null,
            string? descriptionHy = null,
            string? descriptionEn = null,
            string? descriptionRu = null)
        {
            Id = Guid.NewGuid();
            SalonId = salonId;
            Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException("Resource name is required.", nameof(name));
            NameHy = !string.IsNullOrWhiteSpace(nameHy) ? nameHy.Trim() : Name;
            NameEn = !string.IsNullOrWhiteSpace(nameEn) ? nameEn.Trim() : Name;
            NameRu = !string.IsNullOrWhiteSpace(nameRu) ? nameRu.Trim() : Name;
            Quantity = quantity >= 0 ? quantity : throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
            Description = description?.Trim();
            DescriptionHy = descriptionHy?.Trim() ?? Description;
            DescriptionEn = descriptionEn?.Trim() ?? Description;
            DescriptionRu = descriptionRu?.Trim() ?? Description;
            IsActive = isActive;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Update(
            string name,
            int quantity,
            string? description = null,
            bool isActive = true,
            string? nameHy = null,
            string? nameEn = null,
            string? nameRu = null,
            string? descriptionHy = null,
            string? descriptionEn = null,
            string? descriptionRu = null)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException("Resource name is required.", nameof(name));
            NameHy = !string.IsNullOrWhiteSpace(nameHy) ? nameHy.Trim() : Name;
            NameEn = !string.IsNullOrWhiteSpace(nameEn) ? nameEn.Trim() : NameEn;
            NameRu = !string.IsNullOrWhiteSpace(nameRu) ? nameRu.Trim() : NameRu;
            Quantity = quantity >= 0 ? quantity : throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
            Description = description?.Trim();
            DescriptionHy = descriptionHy?.Trim() ?? Description;
            DescriptionEn = descriptionEn?.Trim() ?? DescriptionEn;
            DescriptionRu = descriptionRu?.Trim() ?? DescriptionRu;
            IsActive = isActive;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetActive(bool isActive)
        {
            IsActive = isActive;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
