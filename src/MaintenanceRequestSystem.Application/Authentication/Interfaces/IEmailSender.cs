using MaintenanceRequestSystem.Application.Authentication.Models;

namespace MaintenanceRequestSystem.Application.Authentication.Interfaces;

public interface IEmailSender
{
    Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default);
}
