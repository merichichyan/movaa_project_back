using movaa_project_back.Domain.Enums;

namespace movaa_project_back.Domain.Entities;

public class SpecialistSocialLink
{
    public Guid Id { get; private set; }
    public Guid SpecialistId { get; private set; }
    public SocialPlatform Platform { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; } = 0;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected SpecialistSocialLink() { }

    public SpecialistSocialLink(Guid specialistId, SocialPlatform platform, string url, int displayOrder = 0)
    {
        Id = Guid.NewGuid();
        SpecialistId = specialistId;
        Platform = platform;
        Url = url.Trim();
        DisplayOrder = displayOrder;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(SocialPlatform platform, string url, int? displayOrder = null)
    {
        Platform = platform;
        Url = url.Trim();
        if (displayOrder.HasValue)
        {
            DisplayOrder = displayOrder.Value;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDisplayOrder(int order)
    {
        DisplayOrder = order;
        UpdatedAt = DateTime.UtcNow;
    }
}
