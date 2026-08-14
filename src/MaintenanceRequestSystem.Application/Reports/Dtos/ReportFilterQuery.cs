namespace MaintenanceRequestSystem.Application.Reports.Dtos;

public sealed class ReportFilterQuery
{
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? AssignedTechnicianId { get; init; }
}
