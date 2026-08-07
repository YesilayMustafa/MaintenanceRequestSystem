using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed partial class TicketService
{
    /// <summary>
    /// Aktif kullanıcı ve cihaz doğrulamalarından sonra yeni ticket oluşturur.
    /// </summary>
    public async Task<TicketDto> CreateAsync(
        Guid createdByUserId,
        CreateTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(
            createdByUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        ArgumentNullException.ThrowIfNull(request);

        EnsureValidId(
    request.AssetId,
    "Geçerli bir cihaz kimliği gereklidir.");

        var user =
            await _userRepository.GetByIdAsync(
                createdByUserId,
                cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "Talebi oluşturan kullanıcı bulunamadı.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException(
                "Pasif kullanıcılar talep oluşturamaz.");
        }

        var asset =
            await _assetRepository.GetByIdAsync(
                request.AssetId,
                cancellationToken);

        if (asset is null)
        {
            throw new KeyNotFoundException(
                "Seçilen cihaz bulunamadı.");
        }

        if (!asset.IsActive)
        {
            throw new RequestValidationException(
                "Pasif bir cihaz için yeni talep oluşturulamaz.");
        }

        var ticket = new Ticket(
            request.AssetId,
            createdByUserId,
            request.Title,
            request.Description,
            request.Priority);

        await _ticketRepository.AddAsync(
            ticket,
            cancellationToken);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(
            ticket,
            asset,
            user);
    }
}
