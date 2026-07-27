using MaintenanceRequestSystem.Application.Departments.Dtos;

namespace MaintenanceRequestSystem.Application.Departments.Interfaces;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<DepartmentDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DepartmentDto> CreateAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<DepartmentDto?> UpdateAsync(
        Guid id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> ChangeStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default);
}