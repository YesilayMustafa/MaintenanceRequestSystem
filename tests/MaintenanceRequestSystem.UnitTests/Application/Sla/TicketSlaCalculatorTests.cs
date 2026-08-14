using System.Reflection;
using MaintenanceRequestSystem.Application.Sla.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Sla;

public sealed class TicketSlaCalculatorTests
{
    [Theory]
    [InlineData(TicketPriority.Critical, 4)]
    [InlineData(TicketPriority.High, 24)]
    [InlineData(TicketPriority.Medium, 48)]
    [InlineData(TicketPriority.Low, 72)]
    public void Constructor_SetsDeadlineFromPriority(
        TicketPriority priority,
        int expectedHours)
    {
        var ticket = CreateTicket(priority);

        Assert.Equal(
            TimeSpan.FromHours(expectedHours),
            ticket.SlaDueAt - ticket.CreatedAt);
    }

    [Fact]
    public void ChangePriority_RecalculatesDeadlineFromOriginalCreatedAt()
    {
        var ticket = CreateTicket(TicketPriority.Medium);

        ticket.ChangePriority(
            TicketPriority.High,
            Guid.NewGuid(),
            TimeSpan.FromHours(24));

        Assert.Equal(ticket.CreatedAt.AddHours(24), ticket.SlaDueAt);
    }

    [Fact]
    public void Calculate_ActiveBeforeDueSoonWindow_ReturnsOnTrack()
    {
        var ticket = CreateTicket(TicketPriority.High);

        var result = TicketSlaCalculator.Calculate(
            ticket,
            ticket.CreatedAt.AddHours(12));

        Assert.Equal(SlaStatus.OnTrack, result.Status);
        Assert.Equal(720, result.RemainingMinutes);
    }

    [Fact]
    public void Calculate_ActiveInsideDueSoonWindow_ReturnsDueSoon()
    {
        var ticket = CreateTicket(TicketPriority.High);

        var result = TicketSlaCalculator.Calculate(
            ticket,
            ticket.SlaDueAt.AddHours(-2));

        Assert.Equal(SlaStatus.DueSoon, result.Status);
    }

    [Fact]
    public void Calculate_ActiveAfterDeadline_ReturnsBreached()
    {
        var ticket = CreateTicket(TicketPriority.High);

        var result = TicketSlaCalculator.Calculate(
            ticket,
            ticket.SlaDueAt.AddMinutes(1));

        Assert.Equal(SlaStatus.Breached, result.Status);
        Assert.Equal(-1, result.RemainingMinutes);
    }

    [Fact]
    public void Calculate_ResolvedBeforeDeadline_ReturnsMet()
    {
        var ticket = CreateTicket(TicketPriority.High);
        SetProperty(ticket, nameof(Ticket.Status), TicketStatus.Resolved);
        SetProperty(
            ticket,
            nameof(Ticket.ResolvedAt),
            (DateTime?)ticket.SlaDueAt.AddMinutes(-1));

        var result = TicketSlaCalculator.Calculate(
            ticket,
            ticket.SlaDueAt.AddDays(1));

        Assert.Equal(SlaStatus.Met, result.Status);
    }

    [Fact]
    public void Calculate_ResolvedAfterDeadline_ReturnsBreached()
    {
        var ticket = CreateTicket(TicketPriority.High);
        SetProperty(ticket, nameof(Ticket.Status), TicketStatus.Resolved);
        SetProperty(
            ticket,
            nameof(Ticket.ResolvedAt),
            (DateTime?)ticket.SlaDueAt.AddMinutes(1));

        var result = TicketSlaCalculator.Calculate(
            ticket,
            ticket.SlaDueAt.AddDays(1));

        Assert.Equal(SlaStatus.Breached, result.Status);
    }

    [Fact]
    public void Calculate_CancelledTicket_ReturnsNotApplicable()
    {
        var ticket = CreateTicket(TicketPriority.High);
        SetProperty(ticket, nameof(Ticket.Status), TicketStatus.Cancelled);

        var result = TicketSlaCalculator.Calculate(
            ticket,
            ticket.SlaDueAt.AddDays(1));

        Assert.Equal(SlaStatus.NotApplicable, result.Status);
        Assert.Null(result.RemainingMinutes);
    }

    private static Ticket CreateTicket(TicketPriority priority)
    {
        return new Ticket(
            "REQ-2026-900001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SLA testi",
            "SLA davranışı test açıklaması.",
            priority);
    }

    private static void SetProperty<T>(
        Ticket ticket,
        string propertyName,
        T value)
    {
        typeof(Ticket)
            .GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(ticket, value);
    }
}
