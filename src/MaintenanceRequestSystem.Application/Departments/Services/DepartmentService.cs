using MaintenanceRequestSystem.Application.Departments.Dtos;
using MaintenanceRequestSystem.Application.Departments.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Application.Common.Exceptions;

namespace MaintenanceRequestSystem.Application.Departments.Services;

public sealed class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;

    public DepartmentService(
        IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var departments =
            await _departmentRepository.GetAllAsync(cancellationToken);

        return departments
            .Select(MapToDto)
            .ToList();
    }

    public async Task<DepartmentDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var department =
            await _departmentRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (department is null)
        {
            return null;
        }

        return MapToDto(department);
    }

    public async Task<DepartmentDto> CreateAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new RequestValidationException(
                "Departman adı boş olamaz.");
        }

        var nameExists =
            await _departmentRepository.ExistsByNameAsync(
                name,
                cancellationToken: cancellationToken);

        if (nameExists)
        {
            throw new ConflictException(
             "Aynı isimde bir departman zaten bulunmaktadır.");
        }

        var description = NormalizeDescription(request.Description);

        var department = new Department(
            name,
            description);

        await _departmentRepository.AddAsync(
            department,
            cancellationToken);

        await _departmentRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(department);
    }

    public async Task<DepartmentDto?> UpdateAsync(
        Guid id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var department =
            await _departmentRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (department is null)
        {
            return null;
        }

        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new RequestValidationException(
             "Departman adı boş olamaz.");
        }

        var nameExists =
            await _departmentRepository.ExistsByNameAsync(
                name,
                excludedDepartmentId: id,
                cancellationToken: cancellationToken);

        if (nameExists)
        {
            throw new ConflictException(
             "Aynı isimde başka bir departman bulunmaktadır.");
        }

        var description = NormalizeDescription(request.Description);

        department.UpdateDetails(
            name,
            description);

        await _departmentRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(department);
    }

    public async Task<bool> ChangeStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var department =
            await _departmentRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (department is null)
        {
            return false;
        }

        if (isActive)
        {
            department.Activate();
        }
        else
        {
            department.Deactivate();
        }

        await _departmentRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    private static DepartmentDto MapToDto(
        Department department)
    {
        return new DepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt
        };
    }

    private static string? NormalizeDescription(
        string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }
}