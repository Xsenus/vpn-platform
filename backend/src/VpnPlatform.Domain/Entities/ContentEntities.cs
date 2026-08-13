using VpnPlatform.Domain.Common;

namespace VpnPlatform.Domain.Entities;

public class FaqEntry : AuditableEntity
{
    public int Revision { get; set; }
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
    public int Revision { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Group { get; set; } = "home";
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InputType { get; set; } = "text";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 100;
}

public class WorkScenario : AuditableEntity
{
    public int Revision { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string AllowedTariffIdsJson { get; set; } = "[]";
    public string VpnProtocol { get; set; } = "vless";
    public string ServerSelectionRule { get; set; } = "least-loaded";
    public string InboundSelectionRule { get; set; } = "default";
    public string ProvisioningMode { get; set; } = "auto";
    public string OnPaymentSucceeded { get; set; } = "create_subscription_and_access";
    public string OnPaymentFailed { get; set; } = "keep_order_pending";
    public string OnRefund { get; set; } = "disable_access";
    public string OnSubscriptionExpired { get; set; } = "disable_access_after_grace";
    public string OnRenewal { get; set; } = "extend_subscription";
    public string CabinetText { get; set; } = string.Empty;
    public string TelegramText { get; set; } = string.Empty;
    public bool GenerateQrCode { get; set; } = true;
    public int MaxDevices { get; set; } = 3;
    public long? TrafficLimit { get; set; }
    public int SortOrder { get; set; } = 100;
}
