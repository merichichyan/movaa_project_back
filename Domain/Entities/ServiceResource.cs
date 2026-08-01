using System;

namespace movaa_project_back.Domain.Entities
{
    public class ServiceResource
    {
        public Guid Id { get; private set; }
        public Guid SalonId { get; private set; }
        public string ServiceId { get; private set; }
        public string? ServiceName { get; private set; }
        public Guid ResourceId { get; private set; }
        public int RequiredQuantity { get; private set; }

        public SalonResource? Resource { get; private set; }

        private ServiceResource() { }

        public ServiceResource(
            Guid salonId,
            string serviceId,
            Guid resourceId,
            int requiredQuantity = 1,
            string? serviceName = null)
        {
            Id = Guid.NewGuid();
            SalonId = salonId;
            ServiceId = !string.IsNullOrWhiteSpace(serviceId) ? serviceId.Trim() : throw new ArgumentException("ServiceId is required.", nameof(serviceId));
            ServiceName = serviceName?.Trim();
            ResourceId = resourceId;
            RequiredQuantity = requiredQuantity > 0 ? requiredQuantity : 1;
        }

        public void UpdateRequiredQuantity(int quantity)
        {
            RequiredQuantity = quantity > 0 ? quantity : 1;
        }

        public void UpdateServiceName(string? serviceName)
        {
            ServiceName = serviceName?.Trim();
        }
    }
}
