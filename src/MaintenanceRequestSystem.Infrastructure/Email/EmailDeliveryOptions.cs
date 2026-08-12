namespace MaintenanceRequestSystem.Infrastructure.Email;

public sealed class EmailDeliveryOptions
{
    public const string SectionName = "Email";

    public string Mode { get; init; } = string.Empty;
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
    public bool EnableSsl { get; init; } = true;
    public string? DevelopmentDirectory { get; init; }
}
