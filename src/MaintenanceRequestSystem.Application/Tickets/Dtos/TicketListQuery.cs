using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Dtos;

public sealed class TicketListQuery
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public TicketStatus? Status { get; init; }

    public TicketPriority? Priority { get; init; }

    public Guid? AssetId { get; init; }

    public string? TicketNumber { get; init; }

    public string SortBy { get; init; } = "createdAt";

    public bool SortDescending { get; init; } = true;
}
