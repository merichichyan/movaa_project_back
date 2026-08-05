namespace movaa_project_back.Domain.Entities;

public class SpecialistPhoneChangeRequest
{
    public Guid Id { get; private set; }
    public Guid SpecialistId { get; private set; }
    public string SpecialistName { get; private set; } = string.Empty;
    public string OldPrimaryPhone { get; private set; } = string.Empty;
    public string OldAdditionalPhonesJson { get; private set; } = "[]";
    public string NewPrimaryPhone { get; private set; } = string.Empty;
    public string NewAdditionalPhonesJson { get; private set; } = "[]";
    public string Status { get; private set; } = "Pending"; // Pending, Approved, Rejected
    public string? RejectionNote { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected SpecialistPhoneChangeRequest() { }

    public SpecialistPhoneChangeRequest(
        Guid specialistId,
        string specialistName,
        string oldPrimaryPhone,
        string? oldAdditionalPhonesJson,
        string newPrimaryPhone,
        string? newAdditionalPhonesJson)
    {
        Id = Guid.NewGuid();
        SpecialistId = specialistId;
        SpecialistName = specialistName.Trim();
        OldPrimaryPhone = oldPrimaryPhone.Trim();
        OldAdditionalPhonesJson = !string.IsNullOrWhiteSpace(oldAdditionalPhonesJson) ? oldAdditionalPhonesJson.Trim() : "[]";
        NewPrimaryPhone = newPrimaryPhone.Trim();
        NewAdditionalPhonesJson = !string.IsNullOrWhiteSpace(newAdditionalPhonesJson) ? newAdditionalPhonesJson.Trim() : "[]";
        Status = "Pending";
        CreatedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        Status = "Approved";
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string? note = null)
    {
        Status = "Rejected";
        RejectionNote = note?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
