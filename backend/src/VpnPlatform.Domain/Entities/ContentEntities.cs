using VpnPlatform.Domain.Common;

namespace VpnPlatform.Domain.Entities;

public class FaqEntry : AuditableEntity
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Category { get; set; } = "Общее";
    public bool IsActive { get; set; } = true;
    public bool ShowOnHome { get; set; } = true;
    public bool ShowOnFaqPage { get; set; } = true;
    public int SortOrder { get; set; } = 100;
}

public class SiteContentBlock : AuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Group { get; set; } = "home";
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InputType { get; set; } = "text";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 100;
}
