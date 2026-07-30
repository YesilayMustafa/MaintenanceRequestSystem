using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Dtos;

public sealed class CreateTicketRequest
{
    public Guid AssetId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public TicketPriority Priority { get; init; }
}