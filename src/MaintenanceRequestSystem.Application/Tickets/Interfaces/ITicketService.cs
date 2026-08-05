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


    /// <summary>
    /// Atanmış bir talebi, yalnızca talebe atanmış aktif teknik personelin
    /// işleme almasını sağlar.
    /// </summary>
    Task<TicketDto> StartProgressAsync(
    Guid id,
    Guid currentUserId,
    UserRole currentUserRole,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// İşlemdeki talebi, atanmış teknik personel adına beklemeye alır.
    /// </summary>
    Task<TicketDto> PutOnHoldAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        PutTicketOnHoldRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Beklemedeki talepte, atanmış teknik personel adına işleme devam eder.
    /// </summary>
    Task<TicketDto> ResumeAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// İşlemdeki talebi, atanmış teknik personelin çözüm açıklamasıyla
    /// Resolved durumuna geçirmesini sağlar.
    /// </summary>
    Task<TicketDto> ResolveAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        ResolveTicketRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Çözümlenmiş talebi, talep sahibi veya Admin adına kapatır.
    /// </summary>
    Task<TicketDto> CloseAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Kapatılmış talebi, talep sahibi veya Admin adına yeniden açar.
    /// </summary>
    Task<TicketDto> ReopenAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        ReopenTicketRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// İptale uygun talebi, talep sahibi Employee veya Admin adına iptal eder.
    /// </summary>
    Task<TicketDto> CancelAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aktif durumdaki talebin önceliğini Admin adına değiştirir.
    /// </summary>
    Task<TicketDto> ChangePriorityAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        ChangeTicketPriorityRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kapatılmış veya iptal edilmiş talebi Admin adına
    /// soft delete ile pasifleştirir.
    /// </summary>
    Task SoftDeleteAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının erişebildiği talebin durum geçmişini getirir.
    /// </summary>
    Task<IReadOnlyList<TicketHistoryDto>> GetHistoryAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);
}
