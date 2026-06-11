using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Enums;
using Xunit;

namespace VpnPlatform.UnitTests;

public class StatusStateMachineTests
{
    [Theory]
    [InlineData(OrderStatus.PendingPayment, OrderStatus.PaymentReceived)]
    [InlineData(OrderStatus.PaymentReceived, OrderStatus.Completed)]
    [InlineData(OrderStatus.Failed, OrderStatus.PendingPayment)]
    [InlineData(OrderStatus.Completed, OrderStatus.Refunded)]
    public void Order_State_Machine_Should_Allow_Business_Transitions(OrderStatus from, OrderStatus to)
        => Assert.True(StatusStateMachine.CanTransition(from, to));

    [Theory]
    [InlineData(OrderStatus.Completed, OrderStatus.PendingPayment)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.PaymentReceived)]
    [InlineData(OrderStatus.Refunded, OrderStatus.Completed)]
    public void Order_State_Machine_Should_Block_Impossible_Transitions(OrderStatus from, OrderStatus to)
        => Assert.False(StatusStateMachine.CanTransition(from, to));

    [Theory]
    [InlineData(PaymentStatus.New, PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.Succeeded, PaymentStatus.PartiallyRefunded)]
    [InlineData(PaymentStatus.PartiallyRefunded, PaymentStatus.Refunded)]
    public void Payment_State_Machine_Should_Allow_Business_Transitions(PaymentStatus from, PaymentStatus to)
        => Assert.True(StatusStateMachine.CanTransition(from, to));

    [Theory]
    [InlineData(PaymentStatus.Succeeded, PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Succeeded, PaymentStatus.Cancelled)]
    [InlineData(PaymentStatus.Refunded, PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.Cancelled, PaymentStatus.Succeeded)]
    public void Payment_State_Machine_Should_Block_Impossible_Transitions(PaymentStatus from, PaymentStatus to)
        => Assert.False(StatusStateMachine.CanTransition(from, to));

    [Theory]
    [InlineData(SubscriptionStatus.PendingActivation, SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.GracePeriod)]
    [InlineData(SubscriptionStatus.GracePeriod, SubscriptionStatus.Expired)]
    [InlineData(SubscriptionStatus.Expired, SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.Blocked, SubscriptionStatus.Active)]
    public void Subscription_State_Machine_Should_Allow_Business_Transitions(SubscriptionStatus from, SubscriptionStatus to)
        => Assert.True(StatusStateMachine.CanTransition(from, to));

    [Theory]
    [InlineData(SubscriptionStatus.Cancelled, SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.Cancelled, SubscriptionStatus.Expired)]
    [InlineData(SubscriptionStatus.Expired, SubscriptionStatus.GracePeriod)]
    public void Subscription_State_Machine_Should_Block_Impossible_Transitions(SubscriptionStatus from, SubscriptionStatus to)
        => Assert.False(StatusStateMachine.CanTransition(from, to));

    [Theory]
    [InlineData(AccessCredentialStatus.Provisioning, AccessCredentialStatus.Active)]
    [InlineData(AccessCredentialStatus.Active, AccessCredentialStatus.Disabled)]
    [InlineData(AccessCredentialStatus.Disabled, AccessCredentialStatus.Active)]
    [InlineData(AccessCredentialStatus.Error, AccessCredentialStatus.SyncRequired)]
    public void Access_State_Machine_Should_Allow_Business_Transitions(AccessCredentialStatus from, AccessCredentialStatus to)
        => Assert.True(StatusStateMachine.CanTransition(from, to));

    [Theory]
    [InlineData(AccessCredentialStatus.Revoked, AccessCredentialStatus.Active)]
    [InlineData(AccessCredentialStatus.Revoked, AccessCredentialStatus.Disabled)]
    [InlineData(AccessCredentialStatus.Disabled, AccessCredentialStatus.Provisioning)]
    public void Access_State_Machine_Should_Block_Impossible_Transitions(AccessCredentialStatus from, AccessCredentialStatus to)
        => Assert.False(StatusStateMachine.CanTransition(from, to));

    [Theory]
    [InlineData(ProvisioningRunStatus.PrecheckQueued, ProvisioningRunStatus.Prechecking)]
    [InlineData(ProvisioningRunStatus.Prechecking, ProvisioningRunStatus.ReadyToDeploy)]
    [InlineData(ProvisioningRunStatus.DeployQueued, ProvisioningRunStatus.Deploying)]
    [InlineData(ProvisioningRunStatus.Deploying, ProvisioningRunStatus.Deployed)]
    [InlineData(ProvisioningRunStatus.PrecheckFailed, ProvisioningRunStatus.Retrying)]
    public void Provisioning_State_Machine_Should_Allow_Business_Transitions(ProvisioningRunStatus from, ProvisioningRunStatus to)
        => Assert.True(StatusStateMachine.CanTransition(from, to));

    [Theory]
    [InlineData(ProvisioningRunStatus.Succeeded, ProvisioningRunStatus.Running)]
    [InlineData(ProvisioningRunStatus.Deployed, ProvisioningRunStatus.Deploying)]
    [InlineData(ProvisioningRunStatus.Cancelled, ProvisioningRunStatus.Running)]
    [InlineData(ProvisioningRunStatus.Failed, ProvisioningRunStatus.Succeeded)]
    public void Provisioning_State_Machine_Should_Block_Impossible_Transitions(ProvisioningRunStatus from, ProvisioningRunStatus to)
        => Assert.False(StatusStateMachine.CanTransition(from, to));
}
