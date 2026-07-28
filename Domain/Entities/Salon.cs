using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace movaa_project_back.Domain.Entities;

public class Salon
{
    public Guid Id { get; private set; }
    public string Category { get; private set; } = "Salon";
    public string Name { get; private set; } = string.Empty;
    public string NameHy { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameRu { get; private set; } = string.Empty;

    public string PhoneNumber { get; private set; } = string.Empty;
    public string? Email { get; private set; }

    public string Address { get; private set; } = string.Empty;
    public string AddressHy { get; private set; } = string.Empty;
    public string AddressEn { get; private set; } = string.Empty;
    public string AddressRu { get; private set; } = string.Empty;

    public string WorkingHours { get; private set; } = "09:00 - 18:00";
    public string? LogoUrl { get; private set; }

    public string OwnerFullName { get; private set; } = string.Empty;
    public string? OwnerName { get; private set; }
    public string? OwnerNameHy { get; private set; }
    public string? OwnerNameEn { get; private set; }
    public string? OwnerNameRu { get; private set; }

    public string OwnerPhoneNumber { get; private set; } = string.Empty;
    public string? OwnerPhone { get; private set; }
    public string TaxId { get; private set; } = "00000000";

    public string? Description { get; private set; }
    public string? DescriptionHy { get; private set; }
    public string? DescriptionEn { get; private set; }
    public string? DescriptionRu { get; private set; }

    public bool IsApproved { get; private set; } = false;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public bool IsBlocked { get; private set; } = false;

    // Ignored properties (Not in DB table)
    [NotMapped]
    public double Rating { get; private set; } = 5.0;

    [NotMapped]
    public int ReviewCount { get; private set; } = 0;

    // Compatibility getter for JSON payloads
    [NotMapped]
    [JsonPropertyName("phone")]
    public string Phone => PhoneNumber;

    protected Salon() { }

    public Salon(
        string name,
        string address,
        string phoneNumber,
        string? nameHy = null,
        string? nameEn = null,
        string? nameRu = null,
        string? addressHy = null,
        string? addressEn = null,
        string? addressRu = null,
        string? category = null,
        string? workingHours = null,
        string? email = null,
        string? description = null,
        string? descriptionHy = null,
        string? descriptionEn = null,
        string? descriptionRu = null,
        string? logoUrl = null,
        string? ownerFullName = null,
        string? ownerNameHy = null,
        string? ownerNameEn = null,
        string? ownerNameRu = null,
        string? ownerPhoneNumber = null,
        string? taxId = null,
        bool isApproved = false,
        bool isActive = true,
        bool isBlocked = false)
    {
        Id = Guid.NewGuid();
        Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : "Salon Name";
        NameHy = !string.IsNullOrWhiteSpace(nameHy) ? nameHy.Trim() : Name;
        NameEn = !string.IsNullOrWhiteSpace(nameEn) ? nameEn.Trim() : NameHy;
        NameRu = !string.IsNullOrWhiteSpace(nameRu) ? nameRu.Trim() : NameHy;

        Address = !string.IsNullOrWhiteSpace(address) ? address.Trim() : "N/A";
        AddressHy = !string.IsNullOrWhiteSpace(addressHy) ? addressHy.Trim() : Address;
        AddressEn = !string.IsNullOrWhiteSpace(addressEn) ? addressEn.Trim() : AddressHy;
        AddressRu = !string.IsNullOrWhiteSpace(addressRu) ? addressRu.Trim() : AddressHy;

        PhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber) ? phoneNumber.Trim() : "+37400000000";
        Category = !string.IsNullOrWhiteSpace(category) ? category.Trim() : "Salon";
        WorkingHours = !string.IsNullOrWhiteSpace(workingHours) ? workingHours.Trim() : "09:00 - 18:00";
        Email = email?.Trim();

        Description = description?.Trim();
        DescriptionHy = !string.IsNullOrWhiteSpace(descriptionHy) ? descriptionHy.Trim() : Description;
        DescriptionEn = !string.IsNullOrWhiteSpace(descriptionEn) ? descriptionEn.Trim() : DescriptionHy;
        DescriptionRu = !string.IsNullOrWhiteSpace(descriptionRu) ? descriptionRu.Trim() : DescriptionHy;

        LogoUrl = logoUrl?.Trim();
        OwnerFullName = !string.IsNullOrWhiteSpace(ownerFullName) ? ownerFullName.Trim() : Name;
        OwnerName = OwnerFullName;

        OwnerNameHy = !string.IsNullOrWhiteSpace(ownerNameHy) ? ownerNameHy.Trim() : OwnerFullName;
        OwnerNameEn = !string.IsNullOrWhiteSpace(ownerNameEn) ? ownerNameEn.Trim() : OwnerNameHy;
        OwnerNameRu = !string.IsNullOrWhiteSpace(ownerNameRu) ? ownerNameRu.Trim() : OwnerNameHy;

        OwnerPhoneNumber = !string.IsNullOrWhiteSpace(ownerPhoneNumber) ? ownerPhoneNumber.Trim() : PhoneNumber;
        OwnerPhone = OwnerPhoneNumber;
        TaxId = !string.IsNullOrWhiteSpace(taxId) ? taxId.Trim() : "00000000";
        IsApproved = isApproved;
        IsActive = isActive;
        IsBlocked = isBlocked;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name,
        string address,
        string phoneNumber,
        string? nameHy = null,
        string? nameEn = null,
        string? nameRu = null,
        string? addressHy = null,
        string? addressEn = null,
        string? addressRu = null,
        string? category = null,
        string? workingHours = null,
        string? email = null,
        string? description = null,
        string? descriptionHy = null,
        string? descriptionEn = null,
        string? descriptionRu = null,
        string? logoUrl = null,
        string? ownerFullName = null,
        string? ownerNameHy = null,
        string? ownerNameEn = null,
        string? ownerNameRu = null,
        string? ownerPhoneNumber = null,
        string? taxId = null,
        bool? isApproved = null,
        bool? isActive = null,
        bool? isBlocked = null)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name.Trim();
        NameHy = !string.IsNullOrWhiteSpace(nameHy) ? nameHy.Trim() : Name;
        NameEn = !string.IsNullOrWhiteSpace(nameEn) ? nameEn.Trim() : NameHy;
        NameRu = !string.IsNullOrWhiteSpace(nameRu) ? nameRu.Trim() : NameHy;

        if (!string.IsNullOrWhiteSpace(address)) Address = address.Trim();
        AddressHy = !string.IsNullOrWhiteSpace(addressHy) ? addressHy.Trim() : Address;
        AddressEn = !string.IsNullOrWhiteSpace(addressEn) ? addressEn.Trim() : AddressHy;
        AddressRu = !string.IsNullOrWhiteSpace(addressRu) ? addressRu.Trim() : AddressHy;

        if (!string.IsNullOrWhiteSpace(phoneNumber)) PhoneNumber = phoneNumber.Trim();
        if (!string.IsNullOrWhiteSpace(category)) Category = category.Trim();
        if (!string.IsNullOrWhiteSpace(workingHours)) WorkingHours = workingHours.Trim();
        Email = email?.Trim() ?? Email;

        Description = description?.Trim() ?? Description;
        DescriptionHy = !string.IsNullOrWhiteSpace(descriptionHy) ? descriptionHy.Trim() : Description;
        DescriptionEn = !string.IsNullOrWhiteSpace(descriptionEn) ? descriptionEn.Trim() : DescriptionHy;
        DescriptionRu = !string.IsNullOrWhiteSpace(descriptionRu) ? descriptionRu.Trim() : DescriptionHy;

        LogoUrl = logoUrl?.Trim() ?? LogoUrl;
        if (!string.IsNullOrWhiteSpace(ownerFullName)) OwnerFullName = ownerFullName.Trim();
        OwnerName = OwnerFullName;

        OwnerNameHy = !string.IsNullOrWhiteSpace(ownerNameHy) ? ownerNameHy.Trim() : OwnerFullName;
        OwnerNameEn = !string.IsNullOrWhiteSpace(ownerNameEn) ? ownerNameEn.Trim() : OwnerNameHy;
        OwnerNameRu = !string.IsNullOrWhiteSpace(ownerNameRu) ? ownerNameRu.Trim() : OwnerNameHy;

        if (!string.IsNullOrWhiteSpace(ownerPhoneNumber)) OwnerPhoneNumber = ownerPhoneNumber.Trim();
        if (!string.IsNullOrWhiteSpace(taxId)) TaxId = taxId.Trim();
        OwnerPhone = OwnerPhoneNumber;
        if (isApproved.HasValue) IsApproved = isApproved.Value;
        if (isActive.HasValue) IsActive = isActive.Value;
        if (isBlocked.HasValue) IsBlocked = isBlocked.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetBlocked(bool isBlocked)
    {
        IsBlocked = isBlocked;
        UpdatedAt = DateTime.UtcNow;
    }
}
