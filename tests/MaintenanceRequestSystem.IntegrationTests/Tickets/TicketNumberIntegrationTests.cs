using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
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
}
