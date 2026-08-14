using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Domain.Services;

public static class TicketSlaTargets
{
    public static TimeSpan GetDefault(TicketPriority priority)
    {
        return priority switch
        {
            TicketPriority.Critical => TimeSpan.FromHours(4),
            TicketPriority.High => TimeSpan.FromHours(24),
            TicketPriority.Medium => TimeSpan.FromHours(48),
            TicketPriority.Low => TimeSpan.FromHours(72),
            _ => throw new ArgumentOutOfRangeException(
                nameof(priority),
                "Geçerli bir talep önceliği gereklidir.")
        };
    }
}
