using MaintenanceRequestSystem.Application.Assets.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed class TicketCreationService : ITicketCreationService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITicketNumberGenerator _ticketNumberGenerator;

    public TicketCreationService(
        ITicketRepository ticketRepository,
        IAssetRepository assetRepository,
        IUserRepository userRepository,
        ITicketNumberGenerator ticketNumberGenerator)
    {
        _ticketRepository = ticketRepository;
        _assetRepository = assetRepository;
        _userRepository = userRepository;
        _ticketNumberGenerator = ticketNumberGenerator;
    }

    /// <summary>
    /// Aktif kullanıcı ve cihaz doğrulamalarından sonra yeni ticket oluşturur.
    /// </summary>
    public async Task<TicketDto> CreateAsync(
        Guid createdByUserId,
        CreateTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        TicketServiceGuards.EnsureValidId(
            createdByUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        ArgumentNullException.ThrowIfNull(request);

        TicketServiceGuards.EnsureValidId(
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

        var ticketNumber =
            await _ticketNumberGenerator.NextAsync(
                DateTime.UtcNow,
                cancellationToken);

        var ticket = new Ticket(
            ticketNumber,
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

        return TicketDtoMapper.MapToDto(
            ticket,
            asset,
            user);
    }
}
