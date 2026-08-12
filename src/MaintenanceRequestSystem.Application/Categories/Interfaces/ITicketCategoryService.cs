using MaintenanceRequestSystem.Application.Categories.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Categories.Interfaces;

public interface ITicketCategoryService
{
    Task<IReadOnlyList<TicketCategoryDto>> GetAllAsync(
        bool includeInactive,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);

    Task<TicketCategoryDto> GetByIdAsync(
        Guid id,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);

    Task<TicketCategoryDto> CreateAsync(
        Guid performedByUserId,
        UserRole currentUserRole,
        CreateTicketCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<TicketCategoryDto> UpdateAsync(
        Guid id,
        Guid performedByUserId,
        UserRole currentUserRole,
        UpdateTicketCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task ChangeStatusAsync(
        Guid id,
        Guid performedByUserId,
        UserRole currentUserRole,
        ChangeTicketCategoryStatusRequest request,
        CancellationToken cancellationToken = default);
}
