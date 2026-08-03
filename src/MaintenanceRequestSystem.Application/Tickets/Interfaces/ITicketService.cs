using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Application.Common.Models;

namespace MaintenanceRequestSystem.Application.Tickets.Interfaces;

public interface ITicketService
{
    Task<TicketDto> CreateAsync(
        Guid createdByUserId,
        CreateTicketRequest request,
        CancellationToken cancellationToken = default);

    Task<TicketDto> AssignAsync(
    Guid id,
    Guid currentUserId,
    UserRole currentUserRole,
    AssignTicketRequest request,
    CancellationToken cancellationToken = default);

    Task<TicketDto> GetByIdAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);
    Task<PagedResult<TicketDto>> GetPagedAsync(
    Guid currentUserId,
    UserRole currentUserRole,
    TicketListQuery query,
    CancellationToken cancellationToken = default);
}