namespace MaintenanceRequestSystem.Application.Categories.Dtos;

public sealed record TicketCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
