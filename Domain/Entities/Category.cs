namespace movaa_project_back.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public string NameHy { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameRu { get; private set; } = string.Empty;
    public string IconName { get; private set; } = "grid_view_rounded";
    public int DisplayOrder { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected Category() { }

    public Category(
        string nameHy,
        string nameEn,
        string nameRu,
        string iconName = "grid_view_rounded",
        int displayOrder = 0)
    {
        Id = Guid.NewGuid();
        NameHy = nameHy.Trim();
        NameEn = nameEn.Trim();
        NameRu = nameRu.Trim();
        IconName = string.IsNullOrWhiteSpace(iconName) ? "grid_view_rounded" : iconName.Trim();
        DisplayOrder = displayOrder;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string nameHy,
        string nameEn,
        string nameRu,
        string iconName,
        int displayOrder,
        bool isActive)
    {
        NameHy = nameHy.Trim();
        NameEn = nameEn.Trim();
        NameRu = nameRu.Trim();
        IconName = string.IsNullOrWhiteSpace(iconName) ? "grid_view_rounded" : iconName.Trim();
        DisplayOrder = displayOrder;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
