using MaintenanceRequestSystem.Application.Assets.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

/// <summary>
/// Ticket use case'lerini, yetki kontrollerini ve repository koordinasyonunu yürütür.
/// </summary>
public sealed partial class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;




    public TicketService(
        ITicketRepository ticketRepository,
        IAssetRepository assetRepository,
        IUserRepository userRepository,
        IAuditLogService auditLogService)
    {
        _ticketRepository = ticketRepository;
        _assetRepository = assetRepository;
        _userRepository = userRepository;

        ArgumentNullException.ThrowIfNull(auditLogService);

        _auditLogService =
            auditLogService;
    }











    private static void EnsureValidId(
    Guid id,
    string errorMessage)
    {
        if (id == Guid.Empty)
        {
            throw new RequestValidationException(
                errorMessage);
        }
    }

    private static void EnsureSupportedRole(
        UserRole role)
    {
        if (!Enum.IsDefined(
                typeof(UserRole),
                role))
        {
            throw new ForbiddenException(
                "Desteklenmeyen kullanıcı rolü.");
        }
    }

    private static TicketDto MapToDto(
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
