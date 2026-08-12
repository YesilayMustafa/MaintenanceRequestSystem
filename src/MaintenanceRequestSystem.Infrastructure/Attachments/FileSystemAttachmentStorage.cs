using MaintenanceRequestSystem.Application.TicketAttachments.Interfaces;
using MaintenanceRequestSystem.Application.TicketAttachments.Models;

namespace MaintenanceRequestSystem.Infrastructure.Attachments;

public sealed class FileSystemAttachmentStorage : IAttachmentStorage
{
    private readonly string _rootPath;
    private readonly string _rootPathWithSeparator;
    private readonly StringComparison _pathComparison;

    public FileSystemAttachmentStorage(AttachmentSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(settings.StorageRootPath))
        {
            throw new InvalidOperationException(
                "Attachment storage root path yapılandırılmalıdır.");
        }

        _rootPath = Path.GetFullPath(settings.StorageRootPath);
        _rootPathWithSeparator = _rootPath.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        _pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var normalizedExtension = extension.Trim().ToLowerInvariant();

        if (normalizedExtension.Length is < 2 or > 10 ||
            normalizedExtension[0] != '.' ||
            normalizedExtension.Any(character =>
                !char.IsLetterOrDigit(character) && character != '.'))
        {
            throw new ArgumentException(
                "Geçerli bir dosya uzantısı gereklidir.",
                nameof(extension));
        }

        var storageKey = $"{Guid.NewGuid():N}{normalizedExtension}";
        var path = ResolveStoragePath(storageKey);

        try
        {
            await using var target = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            await content.CopyToAsync(target, cancellationToken);
            await target.FlushAsync(cancellationToken);
            return storageKey;
        }
        catch
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = ResolveStoragePath(storageKey);

        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteIfExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = ResolveStoragePath(storageKey);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string ResolveStoragePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            storageKey.Contains('/') ||
            storageKey.Contains('\\') ||
            !string.Equals(
                storageKey,
                Path.GetFileName(storageKey),
                StringComparison.Ordinal) ||
            storageKey.IndexOfAny(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
        {
            throw new InvalidOperationException("Geçersiz attachment storage key.");
        }

        var path = Path.GetFullPath(Path.Combine(_rootPath, storageKey));

        if (!path.StartsWith(_rootPathWithSeparator, _pathComparison))
        {
            throw new InvalidOperationException("Attachment path storage root dışına çıkamaz.");
        }

        return path;
    }
}
