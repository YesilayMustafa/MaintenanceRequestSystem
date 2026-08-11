using System.Text;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.Authentication.Models;

namespace MaintenanceRequestSystem.Infrastructure.Email;

public sealed class DevelopmentFileEmailSender : IEmailSender
{
    private readonly string _mailDirectory;

    public DevelopmentFileEmailSender(EmailDeliveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _mailDirectory =
            string.IsNullOrWhiteSpace(options.DevelopmentDirectory)
                ? Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "MaintenanceRequestSystem",
                    "dev-mail")
                : Path.GetFullPath(options.DevelopmentDirectory);
    }

    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        Directory.CreateDirectory(_mailDirectory);

        var fileName =
            $"{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}.eml.txt";

        var content = new StringBuilder()
            .AppendLine($"To: {message.To}")
            .AppendLine($"Subject: {message.Subject}")
            .AppendLine("Content-Type: text/plain; charset=utf-8")
            .AppendLine()
            .AppendLine(message.TextBody)
            .AppendLine()
            .AppendLine("--- HTML ---")
            .AppendLine(message.HtmlBody)
            .ToString();

        await File.WriteAllTextAsync(
            Path.Combine(_mailDirectory, fileName),
            content,
            Encoding.UTF8,
            cancellationToken);
    }
}
