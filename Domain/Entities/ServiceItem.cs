using System;

namespace movaa_project_back.Domain.Entities
{
    public class ServiceItem
    {
        public Guid Id { get; private set; }
        public Guid? SalonId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string NameHy { get; private set; } = string.Empty;
        public string NameEn { get; private set; } = string.Empty;
        public string NameRu { get; private set; } = string.Empty;
        public string Category { get; private set; } = "General";
        public double Price { get; private set; } = 0.0;
        public int DurationMinutes { get; private set; } = 30;
        public string? Description { get; private set; }
        public string SpecialistIdsJson { get; private set; } = "[]";
        public bool IsActive { get; private set; } = true;
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        protected ServiceItem() { }

        public ServiceItem(
            string name,
            double price,
            string category = "General",
            int durationMinutes = 30,
            Guid? salonId = null,
            string? nameHy = null,
            string? nameEn = null,
            string? nameRu = null,
            string? description = null,
            string? specialistIdsJson = null,
            bool isActive = true)
        {
            Id = Guid.NewGuid();
            Name = name.Trim();
            NameHy = !string.IsNullOrWhiteSpace(nameHy) ? nameHy.Trim() : name.Trim();
            NameEn = !string.IsNullOrWhiteSpace(nameEn) ? nameEn.Trim() : string.Empty;
            NameRu = !string.IsNullOrWhiteSpace(nameRu) ? nameRu.Trim() : string.Empty;
            Category = !string.IsNullOrWhiteSpace(category) ? category.Trim() : "General";
            Price = Math.Max(0, price);
            DurationMinutes = Math.Max(1, durationMinutes);
            SalonId = salonId;
            Description = description?.Trim();
            SpecialistIdsJson = !string.IsNullOrWhiteSpace(specialistIdsJson) ? specialistIdsJson.Trim() : "[]";
            IsActive = isActive;
            CreatedAt = DateTime.UtcNow;
        }

        public void Update(
            string name,
            double price,
            string category,
            int durationMinutes,
            string? nameHy = null,
            string? nameEn = null,
            string? nameRu = null,
            string? description = null,
            string? specialistIdsJson = null,
            bool? isActive = null,
            Guid? salonId = null)
        {
            if (!string.IsNullOrWhiteSpace(name)) Name = name.Trim();
            NameHy = !string.IsNullOrWhiteSpace(nameHy) ? nameHy.Trim() : Name;
            NameEn = !string.IsNullOrWhiteSpace(nameEn) ? nameEn.Trim() : NameEn;
            NameRu = !string.IsNullOrWhiteSpace(nameRu) ? nameRu.Trim() : NameRu;
            if (!string.IsNullOrWhiteSpace(category)) Category = category.Trim();
            Price = Math.Max(0, price);
            DurationMinutes = Math.Max(1, durationMinutes);
            Description = description?.Trim() ?? Description;
            if (specialistIdsJson != null) SpecialistIdsJson = specialistIdsJson.Trim();
            if (isActive.HasValue) IsActive = isActive.Value;
            if (salonId.HasValue) SalonId = salonId.Value;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetSpecialistIdsJson(string json)
        {
            SpecialistIdsJson = !string.IsNullOrWhiteSpace(json) ? json.Trim() : "[]";
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetActive(bool isActive)
        {
            IsActive = isActive;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
