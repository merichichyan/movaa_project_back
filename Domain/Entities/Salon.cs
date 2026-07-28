namespace movaa_project_back.Domain.Entities;

public class Salon
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? OwnerName { get; private set; }
    public string? OwnerPhone { get; private set; }
    public string? TaxId { get; private set; }
    public double Rating { get; private set; } = 5.0;
    public int ReviewCount { get; private set; } = 0;
    public bool IsBlocked { get; private set; } = false;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected Salon() { }

    public Salon(
        string name,
        string address,
        string phone,
        string? email = null,
        string? description = null,
        string? logoUrl = null,
        string? ownerName = null,
        string? ownerPhone = null,
        string? taxId = null,
        double rating = 5.0,
        int reviewCount = 0)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
        Address = address.Trim();
        Phone = phone.Trim();
        Email = email?.Trim();
        Description = description?.Trim();
        LogoUrl = logoUrl?.Trim();
        OwnerName = ownerName?.Trim();
        OwnerPhone = ownerPhone?.Trim();
        TaxId = taxId?.Trim();
        Rating = rating;
        ReviewCount = reviewCount;
        IsBlocked = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name,
        string address,
        string phone,
        string? email,
        string? description,
        string? logoUrl,
        string? ownerName = null,
        string? ownerPhone = null,
        string? taxId = null)
    {
        Name = name.Trim();
        Address = address.Trim();
        Phone = phone.Trim();
        Email = email?.Trim();
        Description = description?.Trim();
        LogoUrl = logoUrl?.Trim();
        OwnerName = ownerName?.Trim();
        OwnerPhone = ownerPhone?.Trim();
        TaxId = taxId?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetBlocked(bool isBlocked)
    {
        IsBlocked = isBlocked;
        UpdatedAt = DateTime.UtcNow;
    }
}
