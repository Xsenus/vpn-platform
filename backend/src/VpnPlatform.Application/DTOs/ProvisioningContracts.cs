namespace VpnPlatform.Application.DTOs;

public sealed record ProvisioningStepResult(string StepName, bool Success, string Output, string? ErrorText = null);

public sealed record ProvisioningExecutionResult(
    bool Success,
    string SummaryLog,
    IReadOnlyCollection<ProvisioningStepResult> Steps,
    string? ArtifactDirectory = null,
    string? ErrorText = null);
