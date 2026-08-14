using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Sla.Services;

public static class TicketSlaCalculator
{
    public static TicketSlaResult Calculate(Ticket ticket, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("SLA hesaplama zamanı UTC olmalıdır.", nameof(utcNow));
        }

        if (ticket.Status == TicketStatus.Cancelled)
        {
            return new TicketSlaResult(SlaStatus.NotApplicable, null);
        }

        var completionTime = ticket.ResolvedAt ?? ticket.ClosedAt;
        var remainingMinutes = (long)Math.Ceiling(
            (ticket.SlaDueAt - (completionTime ?? utcNow)).TotalMinutes);

        if (ticket.Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            return new TicketSlaResult(
                completionTime <= ticket.SlaDueAt
                    ? SlaStatus.Met
                    : SlaStatus.Breached,
                remainingMinutes);
        }

        if (utcNow > ticket.SlaDueAt)
        {
            return new TicketSlaResult(SlaStatus.Breached, remainingMinutes);
        }

        var targetDuration = ticket.SlaDueAt - ticket.CreatedAt;
        var dueSoonThreshold = ticket.SlaDueAt - TimeSpan.FromTicks(
            (long)(targetDuration.Ticks * 0.2));

        return new TicketSlaResult(
            utcNow >= dueSoonThreshold ? SlaStatus.DueSoon : SlaStatus.OnTrack,
            remainingMinutes);
    }
}

public sealed record TicketSlaResult(
    SlaStatus Status,
    long? RemainingMinutes);
