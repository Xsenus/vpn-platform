using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VpnPlatform.Api.Controllers.Webhooks;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Enums;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PaymentWebhooksControllerTests
{
    [Theory]
    [InlineData("yoomoney", PaymentProvider.YooMoney)]
    [InlineData("yookassa", PaymentProvider.YooKassa)]
    [InlineData("robokassa", PaymentProvider.RoboKassa)]
    [InlineData("cloudpayments", PaymentProvider.CloudPayments)]
    [InlineData("tbank", PaymentProvider.TBankAcquiring)]
    [InlineData("prodamus", PaymentProvider.Prodamus)]
    [InlineData("stripe", PaymentProvider.Stripe)]
    [InlineData("paypal", PaymentProvider.PayPal)]
    public async Task Payment_Webhook_Actions_Should_Route_Request_Context_To_Expected_Provider(string action, PaymentProvider expectedProvider)
    {
        var processor = new RecordingWebhookProcessor(Result<string>.Success("processed"));
        var controller = CreateController(processor, "InvId=42&Status=success");

        var result = action switch
        {
            "yoomoney" => await controller.YooMoney(CancellationToken.None),
            "yookassa" => await controller.YooKassa(CancellationToken.None),
            "robokassa" => await controller.RoboKassa(CancellationToken.None),
            "cloudpayments" => await controller.CloudPayments("Pay", CancellationToken.None),
            "tbank" => await controller.TBankAcquiring(CancellationToken.None),
            "prodamus" => await controller.Prodamus(CancellationToken.None),
            "stripe" => await controller.Stripe(CancellationToken.None),
            "paypal" => await controller.PayPal(CancellationToken.None),
            _ => throw new InvalidOperationException($"Unknown test action: {action}")
        };

        Assert.Equal(expectedProvider, processor.Provider);
        Assert.Equal("InvId=42&Status=success", processor.RawBody);
        Assert.Equal("request-123", processor.Headers["X-Request-Id"]);
        Assert.Equal(IPAddress.Loopback.ToString(), processor.Headers["X-Source-IP"]);
        if (expectedProvider == PaymentProvider.CloudPayments)
        {
            Assert.Equal("Pay", processor.Headers["X-CloudPayments-Event"]);
        }

        if (expectedProvider == PaymentProvider.RoboKassa)
        {
            var content = Assert.IsType<ContentResult>(result);
            Assert.Equal("OK42", content.Content);
            Assert.Equal("text/plain; charset=utf-8", content.ContentType);
        }
        else
        {
            Assert.IsType<OkObjectResult>(result);
        }
    }

    [Fact]
    public async Task Payment_Webhook_Action_Should_Return_BadRequest_When_Processor_Rejects_Request()
    {
        var processor = new RecordingWebhookProcessor(Result<string>.Failure("invalid signature"));
        var controller = CreateController(processor, "{}");

        var result = await controller.YooKassa(CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Payment_Webhook_Action_Should_Return_ServiceUnavailable_For_Retryable_Failure()
    {
        var processor = new RecordingWebhookProcessor(Result<string>.Failure("retry later", isRetryable: true));
        var controller = CreateController(processor, "{}");

        var result = await controller.YooKassa(CancellationToken.None);

        var unavailable = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
    }

    private static PaymentWebhooksController CreateController(RecordingWebhookProcessor processor, string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.Headers["X-Request-Id"] = "request-123";
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        return new PaymentWebhooksController(processor)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private sealed class RecordingWebhookProcessor : IPaymentWebhookProcessor
    {
        private readonly Result<string> _result;

        public RecordingWebhookProcessor(Result<string> result) => _result = result;

        public PaymentProvider Provider { get; private set; }
        public string RawBody { get; private set; } = string.Empty;
        public IReadOnlyDictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();

        public Task<Result<string>> ProcessAsync(PaymentProvider provider, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            Provider = provider;
            RawBody = rawBody;
            Headers = headers;
            return Task.FromResult(_result);
        }
    }
}
