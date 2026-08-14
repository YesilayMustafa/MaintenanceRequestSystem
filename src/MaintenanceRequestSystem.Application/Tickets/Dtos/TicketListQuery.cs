using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Dtos;

public sealed class TicketListQuery
{
    public const int MaxSearchLength = 200;

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public TicketStatus? Status { get; init; }

    public TicketPriority? Priority { get; init; }

    public SlaStatus? SlaStatus { get; init; }

    public Guid? AssetId { get; init; }

    public string? TicketNumber { get; init; }

    public string? Search { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? CreatedByUserId { get; init; }

    public Guid? AssignedTechnicianId { get; init; }

    public Guid? DepartmentId { get; init; }

    public DateTime? CreatedFrom { get; init; }

    public DateTime? CreatedTo { get; init; }

    public string SortBy { get; init; } = "createdAt";

    public bool SortDescending { get; init; } = true;
}
