namespace MaintenanceRequestSystem.Application.Departments.Dtos;

public sealed class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}