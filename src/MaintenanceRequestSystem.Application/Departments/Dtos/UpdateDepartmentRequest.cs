namespace MaintenanceRequestSystem.Application.Departments.Dtos;

public sealed class UpdateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}