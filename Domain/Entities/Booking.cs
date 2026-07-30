using System;

namespace movaa_project_back.Domain.Entities
{
    public class Booking
    {
        public Guid Id { get; private set; }
        public Guid SpecialistId { get; private set; }
        public string SpecialistName { get; private set; } = string.Empty;
        public string ServiceName { get; private set; } = string.Empty;
        public decimal Price { get; private set; }
        public int DurationMinutes { get; private set; }
        public DateTime BookingDate { get; private set; }
        public string TimeSlot { get; private set; } = string.Empty;
        public Guid UserId { get; private set; }
        public string UserEmail { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }
        public bool IsNoShow { get; private set; } = false;
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
            string? salonName = null)
        {
            Id = Guid.NewGuid();
            SpecialistId = specialistId;
            SpecialistName = specialistName;
            ServiceName = serviceName;
            Price = price;
            DurationMinutes = durationMinutes;
            BookingDate = bookingDate;
            TimeSlot = timeSlot;
            UserId = userId;
            UserEmail = userEmail.ToLowerInvariant().Trim();
            CreatedAt = DateTime.UtcNow;
            SalonId = salonId;
            SalonName = salonName;
        }

        public void MarkAsNoShow(bool isNoShow)
        {
            IsNoShow = isNoShow;
        }
    }
}
