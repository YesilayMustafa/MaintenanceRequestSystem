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
    private readonly ITicketQueryService _ticketQueryService;
    private readonly ITicketCreationService _ticketCreationService;




    public TicketService(
        ITicketRepository ticketRepository,
        IAssetRepository assetRepository,
        IUserRepository userRepository,
        IAuditLogService auditLogService,
        ITicketQueryService ticketQueryService,
        ITicketCreationService ticketCreationService)
    {
        _ticketRepository = ticketRepository;
        _assetRepository = assetRepository;
        _userRepository = userRepository;

        ArgumentNullException.ThrowIfNull(auditLogService);
        ArgumentNullException.ThrowIfNull(ticketQueryService);
        ArgumentNullException.ThrowIfNull(ticketCreationService);

        _auditLogService =
            auditLogService;

        _ticketQueryService =
            ticketQueryService;

        _ticketCreationService =
            ticketCreationService;
    }











    private static void EnsureValidId(
    Guid id,
    string errorMessage)
    {
        TicketServiceGuards.EnsureValidId(
            id,
            errorMessage);
    }

    private static void EnsureSupportedRole(
        UserRole role)
    {
        TicketServiceGuards.EnsureSupportedRole(
            role);
    }

    private static TicketDto MapToDto(
        Ticket ticket,
        Asset? asset = null,
        User? createdByUser = null,
        User? assignedTechnician = null)
    {
        return TicketDtoMapper.MapToDto(
            ticket,
            asset,
            createdByUser,
            assignedTechnician);
    }





}
