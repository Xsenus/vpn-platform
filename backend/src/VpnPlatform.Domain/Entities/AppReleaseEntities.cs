using System.Text.Json.Serialization;
using VpnPlatform.Domain.Common;

namespace VpnPlatform.Domain.Entities;

public class AppRelease : AuditableEntity
{
    public string ReleaseId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset ReleasedAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = "manual";
    public int Revision { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public Guid? UpdatedByUserId { get; set; }
    public string UpdatedByUserName { get; set; } = string.Empty;

    [JsonIgnore]
    public ICollection<AppReleaseItem> Items { get; set; } = new List<AppReleaseItem>();

    [JsonIgnore]
    public ICollection<AppReleaseSeen> SeenByUsers { get; set; } = new List<AppReleaseSeen>();
}

public class AppReleaseItem : AuditableEntity
{
    public Guid AppReleaseId { get; set; }
    public string Type { get; set; } = "new";
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    [JsonIgnore]
    public AppRelease? AppRelease { get; set; }
}

public class AppReleaseSeen : AuditableEntity
{
    public Guid AppReleaseId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset SeenAt { get; set; }

    [JsonIgnore]
    public AppRelease? AppRelease { get; set; }

    [JsonIgnore]
    public User? User { get; set; }
}
