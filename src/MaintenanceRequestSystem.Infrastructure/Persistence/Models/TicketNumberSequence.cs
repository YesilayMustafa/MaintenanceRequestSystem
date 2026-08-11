namespace MaintenanceRequestSystem.Infrastructure.Persistence.Models;

public sealed class TicketNumberSequence
{
    private TicketNumberSequence()
    {
    }

    public TicketNumberSequence(int year)
    {
        Year = year;
        LastValue = 1;
    }

    public int Year { get; private set; }

    public long LastValue { get; private set; }

    public long Increment()
    {
        LastValue = checked(LastValue + 1);
        return LastValue;
    }
}
