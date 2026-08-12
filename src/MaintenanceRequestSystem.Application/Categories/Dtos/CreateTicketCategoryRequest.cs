namespace MaintenanceRequestSystem.Application.Categories.Dtos;

public sealed class CreateTicketCategoryRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
