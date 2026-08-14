using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Reports.Dtos;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceRequestSystem.IntegrationTests.Reports;

public sealed class ReportIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private static int _ticketSequence = 800000;
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ReportIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOverview_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/reports/overview");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOverview_ByEmployee_ReturnsForbidden()
    {
        var token = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);

        using var request = AuthorizedRequest(
            "/api/reports/overview",
            token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOverview_ByAdmin_ReturnsFilteredAggregates()
    {
        var setup = await SeedReportTicketsAsync();
        var token = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);
        var createdFrom = Uri.EscapeDataString(
            DateTime.UtcNow.AddDays(-3).ToString("O"));
        var createdTo = Uri.EscapeDataString(
            DateTime.UtcNow.AddDays(1).ToString("O"));
        using var request = AuthorizedRequest(
            $"/api/reports/overview?departmentId={setup.DepartmentId}" +
            $"&assignedTechnicianId={setup.TechnicianId}" +
            $"&categoryId={TicketCategory.OtherId}" +
            $"&createdFrom={createdFrom}&createdTo={createdTo}",
            token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<ReportOverviewDto>();
        Assert.NotNull(report);
        Assert.Equal(2, report.Summary.TotalTickets);
        Assert.Equal(1, report.Summary.ActiveTickets);
        Assert.Equal(1, report.Summary.ResolvedTickets);
        Assert.Equal(1, report.Summary.SlaMetCount);
        Assert.Equal(1, report.Summary.SlaBreachedCount);
        Assert.Equal(50m, report.Summary.SlaComplianceRate);
        Assert.Contains(report.ByPriority, item =>
            item.Key == nameof(TicketPriority.Critical) && item.Count == 1);
        Assert.NotEmpty(report.DailyCreationTrend);
        var technician = Assert.Single(report.TechnicianPerformance);
        Assert.Equal(setup.TechnicianId, technician.TechnicianId);
        Assert.Equal(2, technician.AssignedCount);
    }

    [Fact]
    public async Task GetOverview_WithInvalidDateRange_ReturnsBadRequest()
    {
        var token = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);
        using var request = AuthorizedRequest(
            "/api/reports/overview" +
            "?createdFrom=2026-08-15T00%3A00%3A00Z" +
            "&createdTo=2026-08-14T00%3A00%3A00Z",
            token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExportTickets_ByAdmin_ReturnsSafeCsv()
    {
        await SeedReportTicketsAsync();
        var token = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);
        using var request = AuthorizedRequest(
            "/api/reports/tickets/export.csv",
            token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-16", response.Content.Headers.ContentType?.CharSet);
        Assert.NotNull(response.Content.Headers.ContentDisposition);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var preamble = Encoding.Unicode.GetPreamble();
        Assert.Equal([0xFF, 0xFE], preamble);
        Assert.Equal(preamble, bytes[..preamble.Length]);
        var csv = new UnicodeEncoding(
            bigEndian: false,
            byteOrderMark: false,
            throwOnInvalidBytes: true)
            .GetString(bytes[preamble.Length..]);
        Assert.DoesNotContain('\uFEFF', csv);
        Assert.StartsWith("sep=;\r\n", csv, StringComparison.Ordinal);
        using var reader = new StringReader(csv);
        Assert.Equal("sep=;", reader.ReadLine());
        var header = reader.ReadLine();
        Assert.NotNull(header);
        Assert.Equal(
            "\"Talep No\";\"Başlık\";\"Kategori\";\"Durum\";" +
            "\"Öncelik\";\"Açılış\";\"SLA Son\";\"SLA\";" +
            "\"Oluşturan\";\"Departman\";\"Teknisyen\"",
            header);
        Assert.Contains(
            "\"'=1+1; Ağ Yönetimi\nTest\"",
            csv,
            StringComparison.Ordinal);
        Assert.Contains("Ağ", csv, StringComparison.Ordinal);
        Assert.Contains("Diğer", csv, StringComparison.Ordinal);
        Assert.Contains("Çalışanı", csv, StringComparison.Ordinal);
        Assert.Contains("Yönetimi", csv, StringComparison.Ordinal);
        Assert.Contains("SLA Aşıldı", csv, StringComparison.Ordinal);
        Assert.Contains("SLA Karşılandı", csv, StringComparison.Ordinal);
        Assert.Matches(
            "\"\\d{2}\\.\\d{2}\\.\\d{4} \\d{2}:\\d{2} UTC\"",
            csv);
        Assert.DoesNotMatch(
            "\"\\d{4}-\\d{2}-\\d{2}T",
            csv);
    }

    [Fact]
    public async Task ExportTickets_ByEmployee_ReturnsForbidden()
    {
        var token = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);
        using var request = AuthorizedRequest(
            "/api/reports/tickets/export.csv",
            token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<ReportSetup> SeedReportTicketsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var employee = await context.Users.SingleAsync(user =>
            user.Email == CustomWebApplicationFactory.EmployeeEmail);
        var department = await context.Departments.FindAsync(employee.DepartmentId);
        Assert.NotNull(department);
        var technician = new User(
            "Rapor Çalışanı",
            $"report-tech-{Guid.NewGuid():N}@example.com",
            "integration-test-hash",
            UserRole.Technician,
            department.Id);
        var asset = new Asset(
            "Rapor cihazı",
            $"REPORT-{Guid.NewGuid():N}",
            AssetType.Computer,
            department.Id);
        var activeBreached = new Ticket(
            NextTicketNumber(),
            asset.Id,
            TicketCategory.OtherId,
            employee.Id,
            "=1+1; Ağ Yönetimi\nTest",
            "CSV güvenlik testi.",
            TicketPriority.Critical);
        var resolvedMet = new Ticket(
            NextTicketNumber(),
            asset.Id,
            TicketCategory.OtherId,
            employee.Id,
            "Çözülen rapor talebi",
            "Rapor metriği testi.",
            TicketPriority.High);
        var createdAt = DateTime.UtcNow.AddDays(-2);
        SetProperty(activeBreached, nameof(Ticket.CreatedAt), createdAt);
        SetProperty(activeBreached, nameof(Ticket.SlaDueAt), createdAt.AddHours(4));
        SetProperty(activeBreached, nameof(Ticket.Status), TicketStatus.Assigned);
        SetProperty(activeBreached, nameof(Ticket.AssignedTechnicianId), (Guid?)technician.Id);
        SetProperty(resolvedMet, nameof(Ticket.CreatedAt), createdAt.AddHours(1));
        SetProperty(resolvedMet, nameof(Ticket.SlaDueAt), createdAt.AddHours(25));
        SetProperty(resolvedMet, nameof(Ticket.Status), TicketStatus.Resolved);
        SetProperty(resolvedMet, nameof(Ticket.ResolvedAt), (DateTime?)createdAt.AddHours(2));
        SetProperty(resolvedMet, nameof(Ticket.AssignedTechnicianId), (Guid?)technician.Id);

        context.AddRange(technician, asset, activeBreached, resolvedMet);
        await context.SaveChangesAsync();

        return new ReportSetup(department.Id, technician.Id);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        return login.AccessToken;
    }

    private static HttpRequestMessage AuthorizedRequest(
        string uri,
        string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static void SetProperty<T>(Ticket ticket, string name, T value)
    {
        typeof(Ticket)
            .GetProperty(name, BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(ticket, value);
    }

    private static string NextTicketNumber()
    {
        return $"REQ-2026-{Interlocked.Increment(ref _ticketSequence):D6}";
    }

    private sealed record ReportSetup(Guid DepartmentId, Guid TechnicianId);
}
