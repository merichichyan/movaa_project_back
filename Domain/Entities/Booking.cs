namespace MovaaProjectBack.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid? SalonId { get; set; }
    public Salon? Salon { get; set; }

    public Guid SpecialistId { get; set; }
    public Specialist? Specialist { get; set; }

    public string ServiceName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime BookingDateTime { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled, Completed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
