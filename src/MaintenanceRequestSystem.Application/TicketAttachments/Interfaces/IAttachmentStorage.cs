namespace MaintenanceRequestSystem.Application.TicketAttachments.Interfaces;

public interface IAttachmentStorage
{
    Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task DeleteIfExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}
