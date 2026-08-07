using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

internal static class TicketDtoMapper
{
    internal static TicketDto MapToDto(
        Ticket ticket,
        Asset? asset = null,
        User? createdByUser = null,
        User? assignedTechnician = null)
    {

        var ticketAssignedTechnician =
    assignedTechnician ??
    ticket.AssignedTechnician;
        var ticketAsset =
            asset ?? ticket.Asset;

        var ticketCreator =
            createdByUser ?? ticket.CreatedByUser;

        return new TicketDto(
            ticket.Id,
            ticket.Title,
            ticket.Description,
            ticket.Priority.ToString(),
            ticket.Status.ToString(),
            ticket.AssetId,
            ticketAsset.Name,
            ticketAsset.SerialNumber,
            ticket.CreatedByUserId,
            ticketCreator.FullName,
            ticket.AssignedTechnicianId,
            ticketAssignedTechnician?.FullName,
            ticket.WaitingReason,
            ticket.ResolutionDescription,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.ResolvedAt,
            ticket.ClosedAt);
    }
}
