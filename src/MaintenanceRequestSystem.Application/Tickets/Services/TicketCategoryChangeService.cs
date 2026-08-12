using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.Categories.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed class TicketCategoryChangeService
    : ITicketCategoryChangeService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketCategoryRepository _categoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;

    public TicketCategoryChangeService(
        ITicketRepository ticketRepository,
        ITicketCategoryRepository categoryRepository,
        IUserRepository userRepository,
        IAuditLogService auditLogService)
    {
        _ticketRepository = ticketRepository;
        _categoryRepository = categoryRepository;
        _userRepository = userRepository;
        _auditLogService = auditLogService;
    }

    public async Task<TicketDto> ChangeCategoryAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        ChangeTicketCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        TicketServiceGuards.EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");
        TicketServiceGuards.EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");
        TicketServiceGuards.EnsureSupportedRole(currentUserRole);
        ArgumentNullException.ThrowIfNull(request);
        TicketServiceGuards.EnsureValidId(
            request.CategoryId,
            "Geçerli bir kategori kimliği gereklidir.");

        if (currentUserRole != UserRole.Admin)
        {
            throw new ForbiddenException(
                "Talep kategorisini yalnızca yönetici değiştirebilir.");
        }

        var admin = await _userRepository.GetByIdAsync(
            currentUserId,
            cancellationToken);

        if (admin is null)
        {
            throw new KeyNotFoundException("Yönetici kullanıcı bulunamadı.");
        }

        if (!admin.IsActive || admin.Role != UserRole.Admin)
        {
            throw new ForbiddenException(
                "Talep kategorisini yalnızca aktif yöneticiler değiştirebilir.");
        }

        var ticket = await _ticketRepository.GetByIdAsync(
            id,
            cancellationToken)
            ?? throw new KeyNotFoundException("Talep bulunamadı.");

        var category = await _categoryRepository.GetByIdAsync(
            request.CategoryId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Seçilen kategori bulunamadı.");

        if (!category.IsActive)
        {
            throw new RequestValidationException(
                "Talep pasif bir kategoriye taşınamaz.");
        }

        if (ticket.CategoryId == category.Id)
        {
            return TicketDtoMapper.MapToDto(ticket);
        }

        var oldCategoryId = ticket.CategoryId;
        var oldCategoryName = ticket.Category.Name;

        ticket.ChangeCategory(
            category.Id,
            currentUserId,
            oldCategoryName,
            category.Name);

        await _auditLogService.AddAsync(
            currentUserId,
            "TicketCategoryChanged",
            nameof(Ticket),
            ticket.Id.ToString(),
            new
            {
                CategoryId = oldCategoryId,
                CategoryName = oldCategoryName
            },
            new
            {
                CategoryId = category.Id,
                CategoryName = category.Name
            },
            cancellationToken);

        await _ticketRepository.SaveChangesAsync(cancellationToken);

        return TicketDtoMapper.MapToDto(
            ticket,
            category: category);
    }
}
