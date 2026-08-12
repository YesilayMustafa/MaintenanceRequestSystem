using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed partial class TicketManagementIntegrationTests
{
    [Fact]
    public async Task CreateTicket_ReturnsNumberInCreateDetailListAndFilter()
    {
        var setup = await CreateTicketSetupAsync();

        Assert.Matches(
            $"^REQ-{DateTime.UtcNow.Year:D4}-[0-9]{{6}}$",
            setup.Ticket.TicketNumber);

        using var detailRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/tickets/{setup.Ticket.Id}",
            setup.EmployeeToken);

        var detailResponse = await _client.SendAsync(detailRequest);
        detailResponse.EnsureSuccessStatusCode();

        var detail =
            await detailResponse.Content.ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(detail);
        Assert.Equal(setup.Ticket.TicketNumber, detail.TicketNumber);

        var list = await GetPagedTicketsAsync(
            setup.EmployeeToken,
            $"/api/tickets?ticketNumber={setup.Ticket.TicketNumber}");

        Assert.Contains(
            list.Items,
            ticket => ticket.Id == setup.Ticket.Id &&
                ticket.TicketNumber == setup.Ticket.TicketNumber);
    }

    [Fact]
    public async Task ConcurrentTicketCreation_GeneratesUniqueSequentialNumbers()
    {
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);

        var employeeToken = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);

        var departmentId = await GetActiveDepartmentIdAsync(adminToken);
        var asset = await CreateAssetAsync(adminToken, departmentId);

        var requests = Enumerable.Range(1, 10)
            .Select(index =>
            {
                var request = CreateAuthorizedRequest(
                    HttpMethod.Post,
                    "/api/tickets",
                    employeeToken,
                    new CreateTicketRequest
                    {
                        AssetId = asset.Id,
                        Title = $"Eşzamanlı talep {index}",
                        Description = "Paralel numara üretimi doğrulaması.",
                        Priority = TicketPriority.Medium
                    });

                return _client.SendAsync(request);
            })
            .ToArray();

        var responses = await Task.WhenAll(requests);

        Assert.All(
            responses,
            response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));

        var tickets = await Task.WhenAll(
            responses.Select(async response =>
            {
                var ticket =
                    await response.Content.ReadFromJsonAsync<TicketDto>();
                Assert.NotNull(ticket);
                return ticket;
            }));

        Assert.Equal(
            tickets.Length,
            tickets.Select(ticket => ticket.TicketNumber).Distinct().Count());

        var sequences = tickets
            .Select(ticket => long.Parse(ticket.TicketNumber[^6..]))
            .Order()
            .ToArray();

        Assert.All(
            sequences.Zip(sequences.Skip(1)),
            pair => Assert.Equal(pair.First + 1, pair.Second));

        Assert.All(
            tickets,
            ticket => Assert.Matches(
                $"^REQ-{DateTime.UtcNow.Year:D4}-[0-9]{{6}}$",
                ticket.TicketNumber));
    }

    [Fact]
    public async Task PostgreSqlGenerator_UsesTopLevelExecuteScalarCommand()
    {
        var connection = new TicketNumberDbConnection();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        var generator = new TicketNumberGenerator(context);
        var utcNow = new DateTime(2099, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        var first = await generator.NextAsync(utcNow);
        var second = await generator.NextAsync(utcNow);

        Assert.Equal(
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            context.Database.ProviderName);
        Assert.Equal("REQ-2099-000001", first);
        Assert.Equal("REQ-2099-000002", second);
        Assert.NotEqual(first, second);
        Assert.Equal(2, connection.ExecuteScalarCallCount);
        Assert.All(
            connection.CommandTexts,
            commandText =>
            {
                Assert.StartsWith(
                    "INSERT INTO ticket_number_sequences",
                    commandText.TrimStart());
                Assert.Contains("VALUES (@year, 1)", commandText);
                Assert.Contains("RETURNING last_value", commandText);
                Assert.DoesNotContain("WITH next_number", commandText);
            });
    }

    private sealed class TicketNumberDbConnection : DbConnection
    {
        private readonly Dictionary<int, long> _sequences = [];
        private ConnectionState _state = ConnectionState.Closed;

        public List<string> CommandTexts { get; } = [];

        public int ExecuteScalarCallCount { get; private set; }

        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "ticket_number_regression";

        public override string DataSource => "test-double";

        public override string ServerVersion => "18.0";

        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Close()
        {
            _state = ConnectionState.Closed;
        }

        public override void Open()
        {
            _state = ConnectionState.Open;
        }

        public override Task OpenAsync(
            CancellationToken cancellationToken)
        {
            _state = ConnectionState.Open;
            return Task.CompletedTask;
        }

        protected override DbTransaction BeginDbTransaction(
            IsolationLevel isolationLevel)
        {
            throw new NotSupportedException();
        }

        protected override DbCommand CreateDbCommand()
        {
            return new TicketNumberDbCommand(this);
        }

        public long ExecuteScalar(string commandText, int year)
        {
            CommandTexts.Add(commandText);
            ExecuteScalarCallCount++;

            var nextValue = _sequences.GetValueOrDefault(year) + 1;
            _sequences[year] = nextValue;
            return nextValue;
        }
    }

    private sealed class TicketNumberDbCommand : DbCommand
    {
        private readonly TicketNumberDbConnection _connection;
        private readonly TicketNumberDbParameterCollection _parameters = new();

        public TicketNumberDbCommand(TicketNumberDbConnection connection)
        {
            _connection = connection;
        }

        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;

        public override int CommandTimeout { get; set; }

        public override CommandType CommandType { get; set; }

        public override bool DesignTimeVisible { get; set; }

        public override UpdateRowSource UpdatedRowSource { get; set; }

        [AllowNull]
        protected override DbConnection DbConnection
        {
            get => _connection;
            set => throw new NotSupportedException();
        }

        protected override DbParameterCollection DbParameterCollection
            => _parameters;

        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery()
        {
            throw new NotSupportedException();
        }

        public override object ExecuteScalar()
        {
            return ExecuteTicketNumberCommand();
        }

        public override Task<object?> ExecuteScalarAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult<object?>(ExecuteTicketNumberCommand());
        }

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter()
        {
            return new TicketNumberDbParameter();
        }

        protected override DbDataReader ExecuteDbDataReader(
            CommandBehavior behavior)
        {
            throw new InvalidOperationException(
                "Composable query execution is not allowed for this command.");
        }

        private long ExecuteTicketNumberCommand()
        {
            var yearParameter = _parameters
                .Cast<DbParameter>()
                .Single(parameter => parameter.ParameterName == "year");

            return _connection.ExecuteScalar(
                CommandText,
                Convert.ToInt32(yearParameter.Value));
        }
    }

    private sealed class TicketNumberDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }

        public override ParameterDirection Direction { get; set; }
            = ParameterDirection.Input;

        public override bool IsNullable { get; set; }

        [AllowNull]
        public override string ParameterName { get; set; } = string.Empty;

        public override int Size { get; set; }

        [AllowNull]
        public override string SourceColumn { get; set; } = string.Empty;

        public override bool SourceColumnNullMapping { get; set; }

        public override object? Value { get; set; }

        public override void ResetDbType()
        {
        }
    }

    private sealed class TicketNumberDbParameterCollection
        : DbParameterCollection
    {
        private readonly List<DbParameter> _parameters = [];

        public override int Count => _parameters.Count;

        public override object SyncRoot => ((ICollection)_parameters).SyncRoot;

        public override int Add(object value)
        {
            _parameters.Add((DbParameter)value);
            return _parameters.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var value in values)
            {
                Add(value!);
            }
        }

        public override void Clear()
        {
            _parameters.Clear();
        }

        public override bool Contains(object value)
        {
            return _parameters.Contains((DbParameter)value);
        }

        public override bool Contains(string value)
        {
            return IndexOf(value) >= 0;
        }

        public override void CopyTo(Array array, int index)
        {
            ((ICollection)_parameters).CopyTo(array, index);
        }

        public override IEnumerator GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }

        public override int IndexOf(object value)
        {
            return _parameters.IndexOf((DbParameter)value);
        }

        public override int IndexOf(string parameterName)
        {
            return _parameters.FindIndex(parameter =>
                parameter.ParameterName == parameterName);
        }

        public override void Insert(int index, object value)
        {
            _parameters.Insert(index, (DbParameter)value);
        }

        public override void Remove(object value)
        {
            _parameters.Remove((DbParameter)value);
        }

        public override void RemoveAt(int index)
        {
            _parameters.RemoveAt(index);
        }

        public override void RemoveAt(string parameterName)
        {
            _parameters.RemoveAt(IndexOf(parameterName));
        }

        protected override DbParameter GetParameter(int index)
        {
            return _parameters[index];
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            return _parameters[IndexOf(parameterName)];
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            _parameters[index] = value;
        }

        protected override void SetParameter(
            string parameterName,
            DbParameter value)
        {
            var index = IndexOf(parameterName);

            if (index < 0)
            {
                _parameters.Add(value);
                return;
            }

            _parameters[index] = value;
        }
    }
}
