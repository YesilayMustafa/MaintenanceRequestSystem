using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Application.Common.Models;

namespace MaintenanceRequestSystem.Application.Tickets.Interfaces;

/// <summary>
/// Ticket use case'lerinin Application katmanı sözleşmesini tanımlar.
/// </summary>
public interface ITicketService
{
    /// <summary>
    /// Aktif bir kullanıcı ve cihaz için yeni ticket oluşturur.
    /// </summary>
    Task<TicketDto> CreateAsync(
        Guid createdByUserId,
        CreateTicketRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin adına açık bir ticket'ı ilk kez aktif bir Technician kullanıcısına atar;
    /// durum geçişi ve history kaydı domain davranışında birlikte oluşturulur.
    /// </summary>
    Task<TicketDto> AssignAsync(
    Guid id,
    Guid currentUserId,
    UserRole currentUserRole,
    AssignTicketRequest request,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin adına atanmış bir ticket'ı farklı ve aktif bir Technician kullanıcısına yeniden atar;
    /// durum korunurken atama değişikliği history ile birlikte kaydedilir.
    /// </summary>
    Task<TicketDto> ReassignAsync(
    Guid id,
    Guid currentUserId,
    UserRole currentUserRole,
    AssignTicketRequest request,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Rol bazlı erişim kurallarını uygulayarak ticket detayını getirir.
    /// </summary>
    Task<TicketDto> GetByIdAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Ticket listesini rol, filtre, sıralama ve sayfalama kurallarıyla getirir.
    /// </summary>
    Task<PagedResult<TicketDto>> GetPagedAsync(
    Guid currentUserId,
    UserRole currentUserRole,
    TicketListQuery query,
    CancellationToken cancellationToken = default);
}
