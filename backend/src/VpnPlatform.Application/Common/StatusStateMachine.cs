using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Common;

public static class StatusStateMachine
{
    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> OrderTransitions = new Dictionary<OrderStatus, OrderStatus[]>
    {
        [OrderStatus.Draft] = [OrderStatus.PendingPayment, OrderStatus.Cancelled, OrderStatus.Expired, OrderStatus.Failed],
        [OrderStatus.PendingPayment] = [OrderStatus.PaymentReceived, OrderStatus.Failed, OrderStatus.Cancelled, OrderStatus.Expired],
        [OrderStatus.PaymentReceived] = [OrderStatus.FulfillmentInProgress, OrderStatus.Completed, OrderStatus.PartiallyProcessed, OrderStatus.Failed, OrderStatus.Refunded],
        [OrderStatus.FulfillmentInProgress] = [OrderStatus.Completed, OrderStatus.PartiallyProcessed, OrderStatus.Failed],
        [OrderStatus.PartiallyProcessed] = [OrderStatus.FulfillmentInProgress, OrderStatus.PaymentReceived, OrderStatus.Completed, OrderStatus.Failed, OrderStatus.Refunded],
        [OrderStatus.Failed] = [OrderStatus.PendingPayment, OrderStatus.Cancelled],
        [OrderStatus.Completed] = [OrderStatus.Refunded]
    };

    private static readonly IReadOnlyDictionary<PaymentStatus, PaymentStatus[]> PaymentTransitions = new Dictionary<PaymentStatus, PaymentStatus[]>
    {
        [PaymentStatus.New] = [PaymentStatus.Pending, PaymentStatus.WaitingConfirmation, PaymentStatus.Succeeded, PaymentStatus.Failed, PaymentStatus.Cancelled, PaymentStatus.Unknown],
        [PaymentStatus.Pending] = [PaymentStatus.WaitingConfirmation, PaymentStatus.Succeeded, PaymentStatus.Failed, PaymentStatus.Cancelled, PaymentStatus.Unknown],
        [PaymentStatus.WaitingConfirmation] = [PaymentStatus.Pending, PaymentStatus.Succeeded, PaymentStatus.Failed, PaymentStatus.Cancelled, PaymentStatus.Unknown],
        [PaymentStatus.Succeeded] = [PaymentStatus.PartiallyRefunded, PaymentStatus.Refunded],
        [PaymentStatus.PartiallyRefunded] = [PaymentStatus.Refunded],
        [PaymentStatus.Unknown] = [PaymentStatus.Pending, PaymentStatus.WaitingConfirmation, PaymentStatus.Succeeded, PaymentStatus.Failed, PaymentStatus.Cancelled]
    };

    private static readonly IReadOnlyDictionary<SubscriptionStatus, SubscriptionStatus[]> SubscriptionTransitions = new Dictionary<SubscriptionStatus, SubscriptionStatus[]>
    {
        [SubscriptionStatus.PendingActivation] = [SubscriptionStatus.Active, SubscriptionStatus.Expired, SubscriptionStatus.Cancelled, SubscriptionStatus.Blocked],
        [SubscriptionStatus.Active] = [SubscriptionStatus.PendingActivation, SubscriptionStatus.GracePeriod, SubscriptionStatus.Expired, SubscriptionStatus.Suspended, SubscriptionStatus.Cancelled, SubscriptionStatus.Blocked],
        [SubscriptionStatus.GracePeriod] = [SubscriptionStatus.Active, SubscriptionStatus.Expired, SubscriptionStatus.Cancelled, SubscriptionStatus.Blocked],
        [SubscriptionStatus.Expired] = [SubscriptionStatus.Active, SubscriptionStatus.Cancelled, SubscriptionStatus.Blocked],
        [SubscriptionStatus.Suspended] = [SubscriptionStatus.Active, SubscriptionStatus.Expired, SubscriptionStatus.Cancelled, SubscriptionStatus.Blocked],
        [SubscriptionStatus.Blocked] = [SubscriptionStatus.Active, SubscriptionStatus.Expired, SubscriptionStatus.Cancelled]
    };

    private static readonly IReadOnlyDictionary<AccessCredentialStatus, AccessCredentialStatus[]> AccessTransitions = new Dictionary<AccessCredentialStatus, AccessCredentialStatus[]>
    {
        [AccessCredentialStatus.Provisioning] = [AccessCredentialStatus.Active, AccessCredentialStatus.SyncRequired, AccessCredentialStatus.Disabled, AccessCredentialStatus.Revoked, AccessCredentialStatus.Error],
        [AccessCredentialStatus.Active] = [AccessCredentialStatus.Rotating, AccessCredentialStatus.SyncRequired, AccessCredentialStatus.Disabled, AccessCredentialStatus.Revoked, AccessCredentialStatus.Error],
        [AccessCredentialStatus.Rotating] = [AccessCredentialStatus.Active, AccessCredentialStatus.SyncRequired, AccessCredentialStatus.Disabled, AccessCredentialStatus.Revoked, AccessCredentialStatus.Error],
        [AccessCredentialStatus.Disabled] = [AccessCredentialStatus.Active, AccessCredentialStatus.SyncRequired, AccessCredentialStatus.Revoked, AccessCredentialStatus.Error],
        [AccessCredentialStatus.Error] = [AccessCredentialStatus.Active, AccessCredentialStatus.SyncRequired, AccessCredentialStatus.Disabled, AccessCredentialStatus.Revoked],
        [AccessCredentialStatus.SyncRequired] = [AccessCredentialStatus.Active, AccessCredentialStatus.Rotating, AccessCredentialStatus.Disabled, AccessCredentialStatus.Revoked, AccessCredentialStatus.Error]
    };

    private static readonly IReadOnlyDictionary<ProvisioningRunStatus, ProvisioningRunStatus[]> ProvisioningTransitions = new Dictionary<ProvisioningRunStatus, ProvisioningRunStatus[]>
    {
        [ProvisioningRunStatus.Pending] = [ProvisioningRunStatus.Running, ProvisioningRunStatus.Prechecking, ProvisioningRunStatus.Deploying, ProvisioningRunStatus.Cancelled, ProvisioningRunStatus.Failed],
        [ProvisioningRunStatus.Requested] = [ProvisioningRunStatus.AwaitingCredentials, ProvisioningRunStatus.AwaitingConfirmation, ProvisioningRunStatus.PrecheckQueued, ProvisioningRunStatus.Cancelled, ProvisioningRunStatus.Failed],
        [ProvisioningRunStatus.AwaitingCredentials] = [ProvisioningRunStatus.AwaitingConfirmation, ProvisioningRunStatus.PrecheckQueued, ProvisioningRunStatus.Cancelled, ProvisioningRunStatus.Failed],
        [ProvisioningRunStatus.AwaitingConfirmation] = [ProvisioningRunStatus.PrecheckQueued, ProvisioningRunStatus.DeployQueued, ProvisioningRunStatus.Cancelled, ProvisioningRunStatus.Failed],
        [ProvisioningRunStatus.PrecheckQueued] = [ProvisioningRunStatus.Prechecking, ProvisioningRunStatus.Retrying, ProvisioningRunStatus.Cancelled, ProvisioningRunStatus.Failed],
        [ProvisioningRunStatus.Prechecking] = [ProvisioningRunStatus.ReadyToDeploy, ProvisioningRunStatus.PrecheckFailed, ProvisioningRunStatus.Cancelled, ProvisioningRunStatus.Failed],
        [ProvisioningRunStatus.PrecheckFailed] = [ProvisioningRunStatus.Retrying],
        [ProvisioningRunStatus.ReadyToDeploy] = [ProvisioningRunStatus.DeployQueued, ProvisioningRunStatus.Deploying, ProvisioningRunStatus.Cancelled],
        [ProvisioningRunStatus.DeployQueued] = [ProvisioningRunStatus.Deploying, ProvisioningRunStatus.Retrying, ProvisioningRunStatus.Cancelled, ProvisioningRunStatus.Failed],
        [ProvisioningRunStatus.Deploying] = [ProvisioningRunStatus.Deployed, ProvisioningRunStatus.Succeeded, ProvisioningRunStatus.Cancelled, ProvisioningRunStatus.Failed],
        [ProvisioningRunStatus.Deployed] = [ProvisioningRunStatus.Succeeded],
        [ProvisioningRunStatus.Retrying] = [ProvisioningRunStatus.Running, ProvisioningRunStatus.PrecheckQueued, ProvisioningRunStatus.Prechecking, ProvisioningRunStatus.DeployQueued, ProvisioningRunStatus.Deploying, ProvisioningRunStatus.Cancelled, ProvisioningRunStatus.Failed],
        [ProvisioningRunStatus.Running] = [ProvisioningRunStatus.Succeeded, ProvisioningRunStatus.Failed, ProvisioningRunStatus.Cancelled]
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) => CanTransitionCore(OrderTransitions, from, to);
    public static bool CanTransition(PaymentStatus from, PaymentStatus to) => CanTransitionCore(PaymentTransitions, from, to);
    public static bool CanTransition(SubscriptionStatus from, SubscriptionStatus to) => CanTransitionCore(SubscriptionTransitions, from, to);
    public static bool CanTransition(AccessCredentialStatus from, AccessCredentialStatus to) => CanTransitionCore(AccessTransitions, from, to);
    public static bool CanTransition(ProvisioningRunStatus from, ProvisioningRunStatus to) => CanTransitionCore(ProvisioningTransitions, from, to);

    public static Result<string> TrySetOrderStatus(Order order, OrderStatus to, DateTimeOffset now)
        => TrySetStatus(order.Status, to, "Order", value =>
        {
            order.Status = value;
            order.UpdatedAt = now;
        });

    public static Result<string> TrySetPaymentStatus(PaymentAttempt payment, PaymentStatus to, DateTimeOffset now)
        => TrySetStatus(payment.Status, to, "Payment", value =>
        {
            payment.Status = value;
            payment.UpdatedAt = now;
        });

    public static Result<string> TrySetSubscriptionStatus(Subscription subscription, SubscriptionStatus to, DateTimeOffset now)
        => TrySetStatus(subscription.Status, to, "Subscription", value =>
        {
            subscription.Status = value;
            subscription.UpdatedAt = now;
        });

    public static Result<string> TrySetAccessStatus(AccessCredential access, AccessCredentialStatus to, DateTimeOffset now)
        => TrySetStatus(access.Status, to, "Access credential", value =>
        {
            access.Status = value;
            access.UpdatedAt = now;
        });

    public static Result<string> TrySetProvisioningRunStatus(ProvisioningRun run, ProvisioningRunStatus to, DateTimeOffset now)
        => TrySetStatus(run.Status, to, "Provisioning run", value =>
        {
            run.Status = value;
            run.UpdatedAt = now;
        });

    public static void SetOrderStatus(Order order, OrderStatus to, DateTimeOffset now) => Ensure(TrySetOrderStatus(order, to, now));
    public static void SetPaymentStatus(PaymentAttempt payment, PaymentStatus to, DateTimeOffset now) => Ensure(TrySetPaymentStatus(payment, to, now));
    public static void SetSubscriptionStatus(Subscription subscription, SubscriptionStatus to, DateTimeOffset now) => Ensure(TrySetSubscriptionStatus(subscription, to, now));
    public static void SetAccessStatus(AccessCredential access, AccessCredentialStatus to, DateTimeOffset now) => Ensure(TrySetAccessStatus(access, to, now));
    public static void SetProvisioningRunStatus(ProvisioningRun run, ProvisioningRunStatus to, DateTimeOffset now) => Ensure(TrySetProvisioningRunStatus(run, to, now));

    private static Result<string> TrySetStatus<TStatus>(TStatus from, TStatus to, string entityName, Action<TStatus> apply)
        where TStatus : struct, Enum
    {
        if (!CanTransitionDynamic(from, to))
        {
            return Result<string>.Failure($"{entityName} status transition {from} -> {to} is not allowed.");
        }

        apply(to);
        return Result<string>.Success("Status transition applied.");
    }

    private static bool CanTransitionDynamic<TStatus>(TStatus from, TStatus to)
        where TStatus : struct, Enum
        => from switch
        {
            OrderStatus orderFrom when to is OrderStatus orderTo => CanTransition(orderFrom, orderTo),
            PaymentStatus paymentFrom when to is PaymentStatus paymentTo => CanTransition(paymentFrom, paymentTo),
            SubscriptionStatus subscriptionFrom when to is SubscriptionStatus subscriptionTo => CanTransition(subscriptionFrom, subscriptionTo),
            AccessCredentialStatus accessFrom when to is AccessCredentialStatus accessTo => CanTransition(accessFrom, accessTo),
            ProvisioningRunStatus provisioningFrom when to is ProvisioningRunStatus provisioningTo => CanTransition(provisioningFrom, provisioningTo),
            _ => false
        };

    private static bool CanTransitionCore<TStatus>(IReadOnlyDictionary<TStatus, TStatus[]> transitions, TStatus from, TStatus to)
        where TStatus : struct, Enum
        => EqualityComparer<TStatus>.Default.Equals(from, to)
            || (transitions.TryGetValue(from, out var allowed) && allowed.Contains(to));

    private static void Ensure(Result<string> result)
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Error);
        }
    }
}
