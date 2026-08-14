using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Domain.Services;

namespace MaintenanceRequestSystem.Application.Sla.Models;

public sealed class SlaOptions
{
    public const string SectionName = "Sla";

    public int CriticalHours { get; init; } = 4;
    public int HighHours { get; init; } = 24;
    public int MediumHours { get; init; } = 48;
    public int LowHours { get; init; } = 72;

    public TimeSpan GetTarget(TicketPriority priority)
    {
        var hours = priority switch
        {
            TicketPriority.Critical => CriticalHours,
            TicketPriority.High => HighHours,
            TicketPriority.Medium => MediumHours,
            TicketPriority.Low => LowHours,
            _ => (int)TicketSlaTargets.GetDefault(priority).TotalHours
        };

        return TimeSpan.FromHours(hours);
    }
}
