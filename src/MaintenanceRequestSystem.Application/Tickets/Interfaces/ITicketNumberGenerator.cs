namespace MaintenanceRequestSystem.Application.Tickets.Interfaces;

public interface ITicketNumberGenerator
{
    Task<string> NextAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
