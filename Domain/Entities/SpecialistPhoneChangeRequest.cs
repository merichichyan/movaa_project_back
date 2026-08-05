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

    public void Reject(string? note = null, string? noteHy = null, string? noteEn = null, string? noteRu = null)
    {
        Status = "Rejected";
        if (!string.IsNullOrWhiteSpace(noteHy) || !string.IsNullOrWhiteSpace(noteEn) || !string.IsNullOrWhiteSpace(noteRu))
        {
            var dict = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(noteHy)) dict["hy"] = noteHy.Trim();
            if (!string.IsNullOrWhiteSpace(noteEn)) dict["en"] = noteEn.Trim();
            if (!string.IsNullOrWhiteSpace(noteRu)) dict["ru"] = noteRu.Trim();
            RejectionNote = System.Text.Json.JsonSerializer.Serialize(dict);
        }
        else
        {
            RejectionNote = note?.Trim();
        }
        UpdatedAt = DateTime.UtcNow;
    }
}
