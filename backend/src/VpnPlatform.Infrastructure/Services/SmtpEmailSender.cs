using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Infrastructure.Configuration;

namespace VpnPlatform.Infrastructure.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailDeliveryOptions _options;

    public SmtpEmailSender(IOptions<EmailDeliveryOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (!string.Equals(_options.Mode, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Email delivery mode is disabled.");
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = false
        };
        mail.To.Add(new MailAddress(message.ToAddress));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = string.IsNullOrWhiteSpace(_options.Username)
                ? null
                : new NetworkCredential(_options.Username, _options.Password)
        };
        await client.SendMailAsync(mail, cancellationToken);
    }
}
