using System;

namespace movaa_project_back.Domain.Entities
{
    public class Booking
    {
        public Guid Id { get; private set; }
        public Guid SpecialistId { get; private set; }
        public string SpecialistName { get; private set; } = string.Empty;
        public string? ServiceId { get; private set; }
        public string ServiceName { get; private set; } = string.Empty;
        public decimal Price { get; private set; }
        public int DurationMinutes { get; private set; }
        public DateTime BookingDate { get; private set; }
        public string TimeSlot { get; private set; } = string.Empty;
        public Guid UserId { get; private set; }
        public string UserEmail { get; private set; } = string.Empty;

        public string? UserName { get; private set; }
        public string? UserPhone { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public bool IsNoShow { get; private set; } = false;
        public string Status { get; private set; } = "Confirmed";
        public Guid? SalonId { get; private set; }
        public string? SalonName { get; private set; }

        protected Booking() { }

        public Booking(
            Guid specialistId, 
            string specialistName, 
            string serviceName, 
            decimal price, 
            int durationMinutes, 
            DateTime bookingDate, 
            string timeSlot, 
            Guid userId, 
            string userEmail, 
            Guid? salonId = null, 
            string? salonName = null,
            string? serviceId = null,
            string status = "Confirmed",
            string? userName = null,
            string? userPhone = null)
        {
            Id = Guid.NewGuid();
            SpecialistId = specialistId;
            SpecialistName = specialistName;
            ServiceName = serviceName;
            ServiceId = serviceId ?? serviceName;
            Price = price;
            DurationMinutes = durationMinutes;
            BookingDate = bookingDate.Kind == DateTimeKind.Utc ? bookingDate : DateTime.SpecifyKind(bookingDate, DateTimeKind.Utc);
            TimeSlot = timeSlot;
            UserId = userId;
            UserEmail = userEmail.ToLowerInvariant().Trim();
            CreatedAt = DateTime.UtcNow;
            SalonId = salonId;
            SalonName = salonName;
            Status = !string.IsNullOrWhiteSpace(status) ? status : "Confirmed";
            UserName = userName?.Trim();
            UserPhone = userPhone?.Trim();
        }

        public void MarkAsNoShow(bool isNoShow)
        {
            IsNoShow = isNoShow;
        }

        public void SetStatus(string status)
        {
            if (!string.IsNullOrWhiteSpace(status))
            {
                Status = status.Trim();
            }
        }

        public void UpdateDetails(
            Guid? specialistId = null,
            string? specialistName = null,
            string? serviceName = null,
            decimal? price = null,
            int? durationMinutes = null,
            DateTime? bookingDate = null,
            string? timeSlot = null,
            string? status = null,
            Guid? salonId = null,
            string? salonName = null,
            string? userName = null,
            string? userPhone = null)
        {
            if (specialistId.HasValue && specialistId.Value != Guid.Empty) SpecialistId = specialistId.Value;
            if (!string.IsNullOrWhiteSpace(specialistName)) SpecialistName = specialistName;
            if (!string.IsNullOrWhiteSpace(serviceName)) ServiceName = serviceName;
            if (price.HasValue && price.Value > 0) Price = price.Value;
            if (durationMinutes.HasValue && durationMinutes.Value > 0) DurationMinutes = durationMinutes.Value;
            if (bookingDate.HasValue) BookingDate = bookingDate.Value.Kind == DateTimeKind.Utc ? bookingDate.Value : DateTime.SpecifyKind(bookingDate.Value, DateTimeKind.Utc);
            if (!string.IsNullOrWhiteSpace(timeSlot)) TimeSlot = timeSlot;
            if (!string.IsNullOrWhiteSpace(status)) Status = status.Trim();
            if (salonId.HasValue && salonId.Value != Guid.Empty) SalonId = salonId.Value;
            if (!string.IsNullOrWhiteSpace(salonName)) SalonName = salonName;
            if (userName != null) UserName = userName.Trim();
            if (userPhone != null) UserPhone = userPhone.Trim();
        }
    }
}
