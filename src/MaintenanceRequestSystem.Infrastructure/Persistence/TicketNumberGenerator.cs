using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Domain.ValueObjects;
using MaintenanceRequestSystem.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MaintenanceRequestSystem.Infrastructure.Persistence;

public sealed class TicketNumberGenerator : ITicketNumberGenerator
{
    private const string InMemoryProvider =
        "Microsoft.EntityFrameworkCore.InMemory";

    private static readonly SemaphoreSlim InMemorySequenceLock = new(1, 1);

    private readonly ApplicationDbContext _context;

    public TicketNumberGenerator(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> NextAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var year = utcNow.ToUniversalTime().Year;

        var nextValue =
            string.Equals(
                _context.Database.ProviderName,
                InMemoryProvider,
                StringComparison.Ordinal)
                ? await NextInMemoryAsync(year, cancellationToken)
                : await NextPostgreSqlAsync(year, cancellationToken);

        return TicketNumberValue.Create(year, nextValue);
    }

    private async Task<long> NextPostgreSqlAsync(
        int year,
        CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        var shouldCloseConnection =
            connection.State != System.Data.ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();

            command.CommandText =
                """
            INSERT INTO ticket_number_sequences (year, last_value)
            VALUES (@year, 1)
            ON CONFLICT (year) DO UPDATE
            SET last_value = ticket_number_sequences.last_value + 1
            RETURNING last_value;
            """;

            var yearParameter = command.CreateParameter();
            yearParameter.ParameterName = "year";
            yearParameter.Value = year;
            command.Parameters.Add(yearParameter);

            var currentTransaction = _context.Database.CurrentTransaction;

            if (currentTransaction is not null)
            {
                command.Transaction = currentTransaction.GetDbTransaction();
            }

            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result is null || result is DBNull)
            {
                throw new InvalidOperationException(
                    "Ticket number sequence could not be generated.");
            }

            return Convert.ToInt64(result);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<long> NextInMemoryAsync(
        int year,
        CancellationToken cancellationToken)
    {
        await InMemorySequenceLock.WaitAsync(cancellationToken);

        try
        {
            var sequence =
                await _context.TicketNumberSequences.FindAsync(
                    [year],
                    cancellationToken);

            long nextValue;

            if (sequence is null)
            {
                sequence = new TicketNumberSequence(year);
                _context.TicketNumberSequences.Add(sequence);
                nextValue = sequence.LastValue;
            }
            else
            {
                nextValue = sequence.Increment();
            }

            await _context.SaveChangesAsync(cancellationToken);
            return nextValue;
        }
        finally
        {
            InMemorySequenceLock.Release();
        }
    }
}
