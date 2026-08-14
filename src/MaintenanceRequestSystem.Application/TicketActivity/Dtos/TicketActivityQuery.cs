namespace MaintenanceRequestSystem.Application.TicketActivity.Dtos;

public sealed class TicketActivityQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
