namespace MaintenanceRequestSystem.Application.Categories.Dtos;

public sealed class UpdateTicketCategoryRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
