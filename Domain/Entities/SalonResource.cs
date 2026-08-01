using System;

namespace movaa_project_back.Domain.Entities
{
    public class SalonResource
    {
        public Guid Id { get; private set; }
        public Guid SalonId { get; private set; }
        public string Name { get; private set; }
        public int Quantity { get; private set; }
        public string? Description { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private SalonResource() { }

        public SalonResource(
            Guid salonId,
            string name,
            int quantity,
            string? description = null,
            bool isActive = true)
        {
            Id = Guid.NewGuid();
            SalonId = salonId;
            Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException("Resource name is required.", nameof(name));
            Quantity = quantity >= 0 ? quantity : throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
            Description = description?.Trim();
            IsActive = isActive;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Update(
            string name,
            int quantity,
            string? description = null,
            bool isActive = true)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException("Resource name is required.", nameof(name));
            Quantity = quantity >= 0 ? quantity : throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
            Description = description?.Trim();
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
