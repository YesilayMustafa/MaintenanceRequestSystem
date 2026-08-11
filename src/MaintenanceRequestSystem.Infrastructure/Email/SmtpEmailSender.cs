using System.Net;
using System.Net.Mail;
using System.Text;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.Authentication.Models;

namespace MaintenanceRequestSystem.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailDeliveryOptions _options;

    public SmtpEmailSender(EmailDeliveryOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(
                _options.FromAddress,
                _options.FromName,
                Encoding.UTF8),
            Subject = message.Subject,
            SubjectEncoding = Encoding.UTF8,
            Body = message.HtmlBody,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };

        mailMessage.To.Add(message.To);
        mailMessage.AlternateViews.Add(
            AlternateView.CreateAlternateViewFromString(
                message.TextBody,
                Encoding.UTF8,
                "text/plain"));

        using var smtpClient = new SmtpClient(
            _options.Host,
            _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            smtpClient.Credentials = new NetworkCredential(
                _options.Username,
                _options.Password);
        }

        await smtpClient.SendMailAsync(
            mailMessage,
            cancellationToken);
    }
}
