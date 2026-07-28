using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace movaa_project_back.Domain.Entities;

public class Salon
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string Category { get; private set; } = "Salon";
    public string? Email { get; private set; }
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? OwnerFullName { get; private set; }
    public string? OwnerPhoneNumber { get; private set; }
    public string? TaxId { get; private set; }

    [NotMapped]
    public double Rating { get; private set; } = 5.0;

    [NotMapped]
    public int ReviewCount { get; private set; } = 0;

    public bool IsBlocked { get; private set; } = false;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Compatibility properties for backward-compatible JSON payloads
    [NotMapped]
    [JsonPropertyName("phone")]
    public string Phone => PhoneNumber;

    [NotMapped]
    [JsonPropertyName("ownerName")]
    public string? OwnerName => OwnerFullName;

    [NotMapped]
    [JsonPropertyName("ownerPhone")]
    public string? OwnerPhone => OwnerPhoneNumber;

    protected Salon() { }

    public Salon(
        string name,
        string address,
        string phoneNumber,
        string? category = null,
        string? email = null,
        string? description = null,
        string? logoUrl = null,
        string? ownerFullName = null,
        string? ownerPhoneNumber = null,
        string? taxId = null,
        double rating = 5.0,
        int reviewCount = 0)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
        Address = address.Trim();
        PhoneNumber = phoneNumber.Trim();
        Category = !string.IsNullOrWhiteSpace(category) ? category.Trim() : "Salon";
        Email = email?.Trim();
        Description = description?.Trim();
        LogoUrl = logoUrl?.Trim();
        OwnerFullName = ownerFullName?.Trim();
        OwnerPhoneNumber = ownerPhoneNumber?.Trim();
        TaxId = taxId?.Trim();
        Rating = rating;
        ReviewCount = reviewCount;
        IsBlocked = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name,
        string address,
        string phoneNumber,
        string? category,
        string? email,
        string? description,
        string? logoUrl,
        string? ownerFullName = null,
        string? ownerPhoneNumber = null,
        string? taxId = null)
    {
        Name = name.Trim();
        Address = address.Trim();
        PhoneNumber = phoneNumber.Trim();
        if (!string.IsNullOrWhiteSpace(category)) Category = category.Trim();
        Email = email?.Trim();
        Description = description?.Trim();
        LogoUrl = logoUrl?.Trim();
        OwnerFullName = ownerFullName?.Trim();
        OwnerPhoneNumber = ownerPhoneNumber?.Trim();
        TaxId = taxId?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetBlocked(bool isBlocked)
    {
        IsBlocked = isBlocked;
        UpdatedAt = DateTime.UtcNow;
    }
}
