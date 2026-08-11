using System.Globalization;
using System.Text.RegularExpressions;

namespace MaintenanceRequestSystem.Domain.ValueObjects;

public static partial class TicketNumberValue
{
    public const int MaxLength = 15;
    public const long MaxSequenceValue = 999999;

    public static string Create(int year, long sequence)
    {
        if (year is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year),
                "Talep numarası yılı 1 ile 9999 arasında olmalıdır.");
        }

        if (sequence is < 1 or > MaxSequenceValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                $"Yıllık talep sırası 1 ile {MaxSequenceValue} arasında olmalıdır.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"REQ-{year:D4}-{sequence:D6}");
    }

    public static string Normalize(string ticketNumber)
    {
        if (string.IsNullOrWhiteSpace(ticketNumber))
        {
            throw new ArgumentException(
                "Talep numarası boş olamaz.",
                nameof(ticketNumber));
        }

        var normalizedTicketNumber =
            ticketNumber.Trim().ToUpperInvariant();

        if (!TicketNumberPattern().IsMatch(normalizedTicketNumber))
        {
            throw new ArgumentException(
                "Talep numarası REQ-YYYY-NNNNNN formatında olmalıdır.",
                nameof(ticketNumber));
        }

        return normalizedTicketNumber;
    }

    [GeneratedRegex("^REQ-[0-9]{4}-[0-9]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex TicketNumberPattern();
}
