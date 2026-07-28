using MaintenanceRequestSystem.Application.Departments.Dtos;
using MaintenanceRequestSystem.Application.Departments.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MaintenanceRequestSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
namespace MaintenanceRequestSystem.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public sealed class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;



    public DepartmentsController(
        IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<DepartmentDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var departments =
            await _departmentService.GetAllAsync(cancellationToken);

        return Ok(departments);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(DepartmentDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DepartmentDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var department =
            await _departmentService.GetByIdAsync(
                id,
                cancellationToken);

        if (department is null)
        {
            return NotFound(new
            {
                message = "Departman bulunamadı."
            });
        }

        return Ok(department);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ProducesResponseType(
        typeof(DepartmentDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DepartmentDto>> Create(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var department =
            await _departmentService.CreateAsync(
                request,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = department.Id },
            department);
    }

    [ProducesResponseType(
    typeof(DepartmentDto),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DepartmentDto>> Update(
    Guid id,
    [FromBody] UpdateDepartmentRequest request,
    CancellationToken cancellationToken)
    {
        var department =
            await _departmentService.UpdateAsync(
                id,
                request,
                cancellationToken);

        if (department is null)
        {
            return NotFound(new
            {
                message = "Departman bulunamadı."
            });
        }

        return Ok(department);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeDepartmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var updated =
            await _departmentService.ChangeStatusAsync(
                id,
                request.IsActive,
                cancellationToken);

        if (!updated)
        {
            return NotFound(new
            {
                message = "Departman bulunamadı."
            });
        }

        return NoContent();
    }
}   