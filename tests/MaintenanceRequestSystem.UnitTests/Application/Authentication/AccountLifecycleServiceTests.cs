using MaintenanceRequestSystem.Application.AuditLogs.Dtos;
using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.Authentication.Models;
using MaintenanceRequestSystem.Application.Authentication.Services;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Departments.Interfaces;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace MaintenanceRequestSystem.UnitTests.Application.Authentication;

public sealed class AccountLifecycleServiceTests
{
    [Fact]
    public async Task InviteUserAsync_WithValidRequest_CreatesPendingUserTokenAuditAndEmail()
    {
        var harness = CreateHarness();

        var result = await harness.Service.InviteUserAsync(
            harness.Admin.Id,
            CreateInviteRequest(harness.Department.Id));

        var invitedUser = Assert.Single(
            harness.Users.Users,
            user => user.Id != harness.Admin.Id);
        var token = Assert.Single(harness.Tokens.Tokens);
        var email = Assert.Single(harness.Email.Messages);

        Assert.Equal("PendingInvitation", result.AccountStatus);
        Assert.Null(invitedUser.PasswordHash);
        Assert.False(invitedUser.IsOperational);
        Assert.Equal(AccountTokenType.Invitation, token.Type);
        Assert.DoesNotContain("raw-1", token.TokenHash);
        Assert.Contains(
            "https://frontend.example/accept-invitation?token=raw-1",
            email.TextBody);
        Assert.Equal("UserInvited", Assert.Single(harness.Audit.Actions));
        Assert.Equal(1, harness.Users.SaveChangesCallCount);
    }

    [Fact]
    public async Task InviteUserAsync_WithDuplicateEmail_ThrowsConflictException()
    {
        var harness = CreateHarness();
        harness.Users.Users.Add(CreateOperationalUser(
            UserRole.Employee,
            "invited@example.com"));

        await Assert.ThrowsAsync<ConflictException>(() =>
            harness.Service.InviteUserAsync(
                harness.Admin.Id,
                CreateInviteRequest(harness.Department.Id)));

        Assert.Empty(harness.Tokens.Tokens);
        Assert.Empty(harness.Email.Messages);
    }

    [Fact]
    public async Task InviteUserAsync_WithInactiveDepartment_ThrowsValidationException()
    {
        var harness = CreateHarness(departmentActive: false);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            harness.Service.InviteUserAsync(
                harness.Admin.Id,
                CreateInviteRequest(harness.Department.Id)));

        Assert.Empty(harness.Tokens.Tokens);
    }

    [Fact]
    public async Task InviteUserAsync_WithInvalidRole_ThrowsValidationException()
    {
        var harness = CreateHarness();
        var request = CreateInviteRequest(harness.Department.Id) with
        {
            Role = (UserRole)999
        };

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            harness.Service.InviteUserAsync(
                harness.Admin.Id,
                request));
    }

    [Fact]
    public async Task InviteUserAsync_WhenEmailFails_KeepsCommittedUserAndToken()
    {
        var harness = CreateHarness();
        harness.Email.ExceptionToThrow =
            new InvalidOperationException("delivery failed");

        await Assert.ThrowsAsync<EmailDeliveryException>(() =>
            harness.Service.InviteUserAsync(
                harness.Admin.Id,
                CreateInviteRequest(harness.Department.Id)));

        Assert.Equal(2, harness.Users.Users.Count);
        Assert.Single(harness.Tokens.Tokens);
        Assert.Equal(1, harness.Users.SaveChangesCallCount);
    }

    [Fact]
    public async Task ResendInvitationAsync_WithPendingUser_RevokesPreviousToken()
    {
        var harness = CreateHarness();
        var pending = CreatePendingUser();
        harness.Users.Users.Add(pending);
        var oldToken = AddToken(
            harness,
            pending,
            "old-raw",
            AccountTokenType.Invitation);

        await harness.Service.ResendInvitationAsync(
            pending.Id,
            harness.Admin.Id);

        Assert.NotNull(oldToken.RevokedAt);
        Assert.Equal(2, harness.Tokens.Tokens.Count);
        Assert.Equal(
            "UserInvitationResent",
            Assert.Single(harness.Audit.Actions));
        Assert.Single(harness.Email.Messages);
    }

    [Fact]
    public async Task ResendInvitationAsync_WithActiveUser_ThrowsValidationException()
    {
        var harness = CreateHarness();
        var active = CreateOperationalUser(
            UserRole.Employee,
            "active.user@example.com");
        harness.Users.Users.Add(active);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            harness.Service.ResendInvitationAsync(
                active.Id,
                harness.Admin.Id));
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithValidToken_ActivatesAccountOnce()
    {
        var harness = CreateHarness();
        var pending = CreatePendingUser();
        harness.Users.Users.Add(pending);
        var token = AddToken(
            harness,
            pending,
            "accept-raw",
            AccountTokenType.Invitation);

        await harness.Service.AcceptInvitationAsync(
            new AcceptInvitationRequest(
                "accept-raw",
                "NewPassword123!"));

        Assert.True(pending.IsOperational);
        Assert.Equal(AccountStatus.Active, pending.AccountStatus);
        Assert.Equal("hash:NewPassword123!", pending.PasswordHash);
        Assert.NotNull(token.UsedAt);
        Assert.Equal(
            "UserInvitationAccepted",
            Assert.Single(harness.Audit.Actions));

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            harness.Service.AcceptInvitationAsync(
                new AcceptInvitationRequest(
                    "accept-raw",
                    "AnotherPassword1!")));
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithWrongTokenType_ThrowsValidationException()
    {
        var harness = CreateHarness();
        var pending = CreatePendingUser();
        harness.Users.Users.Add(pending);
        AddToken(
            harness,
            pending,
            "wrong-type",
            AccountTokenType.PasswordReset);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            harness.Service.AcceptInvitationAsync(
                new AcceptInvitationRequest(
                    "wrong-type",
                    "NewPassword123!")));
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithRevokedToken_ThrowsValidationException()
    {
        var harness = CreateHarness();
        var pending = CreatePendingUser();
        harness.Users.Users.Add(pending);
        var token = AddToken(
            harness,
            pending,
            "revoked",
            AccountTokenType.Invitation);
        token.Revoke(DateTime.UtcNow);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            harness.Service.AcceptInvitationAsync(
                new AcceptInvitationRequest(
                    "revoked",
                    "NewPassword123!")));
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithInactiveUser_ThrowsValidationException()
    {
        var harness = CreateHarness();
        var pending = CreatePendingUser();
        pending.Deactivate();
        harness.Users.Users.Add(pending);
        AddToken(
            harness,
            pending,
            "inactive",
            AccountTokenType.Invitation);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            harness.Service.AcceptInvitationAsync(
                new AcceptInvitationRequest(
                    "inactive",
                    "NewPassword123!")));
    }

    [Fact]
    public async Task ForgotPasswordAsync_ForDifferentAccountStates_ReturnsSameResponse()
    {
        var activeHarness = CreateHarness();
        var active = CreateOperationalUser(
            UserRole.Employee,
            "active.reset@example.com");
        activeHarness.Users.Users.Add(active);

        var unknownHarness = CreateHarness();

        var pendingHarness = CreateHarness();
        var pending = CreatePendingUser("pending.reset@example.com");
        pendingHarness.Users.Users.Add(pending);

        var inactiveHarness = CreateHarness();
        var inactive = CreateOperationalUser(
            UserRole.Employee,
            "inactive.reset@example.com");
        inactive.Deactivate();
        inactiveHarness.Users.Users.Add(inactive);

        var responses = await Task.WhenAll(
            activeHarness.Service.ForgotPasswordAsync(
                new ForgotPasswordRequest(active.Email)),
            unknownHarness.Service.ForgotPasswordAsync(
                new ForgotPasswordRequest("unknown@example.com")),
            pendingHarness.Service.ForgotPasswordAsync(
                new ForgotPasswordRequest(pending.Email)),
            inactiveHarness.Service.ForgotPasswordAsync(
                new ForgotPasswordRequest(inactive.Email)));

        Assert.All(
            responses,
            response => Assert.Equal(
                AccountLifecycleService.GenericForgotPasswordMessage,
                response.Message));
        Assert.Single(activeHarness.Tokens.Tokens);
        Assert.Single(activeHarness.Email.Messages);
        Assert.Empty(unknownHarness.Tokens.Tokens);
        Assert.Empty(pendingHarness.Tokens.Tokens);
        Assert.Empty(inactiveHarness.Tokens.Tokens);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenEmailFails_ReturnsGenericResponse()
    {
        var harness = CreateHarness();
        var active = CreateOperationalUser(
            UserRole.Employee,
            "mail.failure@example.com");
        harness.Users.Users.Add(active);
        harness.Email.ExceptionToThrow =
            new InvalidOperationException("delivery failed");

        var response = await harness.Service.ForgotPasswordAsync(
            new ForgotPasswordRequest(active.Email));

        Assert.Equal(
            AccountLifecycleService.GenericForgotPasswordMessage,
            response.Message);
        Assert.Single(harness.Tokens.Tokens);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ChangesPasswordAndRevokesOthers()
    {
        var harness = CreateHarness();
        var user = CreateOperationalUser(
            UserRole.Employee,
            "reset@example.com");
        harness.Users.Users.Add(user);
        var token = AddToken(
            harness,
            user,
            "reset-raw",
            AccountTokenType.PasswordReset);
        var otherToken = AddToken(
            harness,
            user,
            "other-reset",
            AccountTokenType.PasswordReset);
        var oldVersion = user.SecurityVersion;

        await harness.Service.ResetPasswordAsync(
            new ResetPasswordRequest(
                "reset-raw",
                "ResetPassword123!"));

        Assert.Equal(oldVersion + 1, user.SecurityVersion);
        Assert.Equal("hash:ResetPassword123!", user.PasswordHash);
        Assert.NotNull(token.UsedAt);
        Assert.NotNull(otherToken.RevokedAt);
        Assert.Equal(
            "UserPasswordReset",
            Assert.Single(harness.Audit.Actions));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithUsedToken_ThrowsValidationException()
    {
        var harness = CreateHarness();
        var user = CreateOperationalUser(
            UserRole.Employee,
            "used.reset@example.com");
        harness.Users.Users.Add(user);
        var token = AddToken(
            harness,
            user,
            "used-reset",
            AccountTokenType.PasswordReset);
        token.Consume(DateTime.UtcNow);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            harness.Service.ResetPasswordAsync(
                new ResetPasswordRequest(
                    "used-reset",
                    "ResetPassword123!")));
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ChangesVersionAndAudits()
    {
        var harness = CreateHarness();
        var user = CreateOperationalUser(
            UserRole.Employee,
            "change@example.com");
        harness.Users.Users.Add(user);
        var oldVersion = user.SecurityVersion;

        await harness.Service.ChangePasswordAsync(
            user.Id,
            new ChangePasswordRequest(
                "OldPassword123!",
                "NewPassword123!"));

        Assert.Equal(oldVersion + 1, user.SecurityVersion);
        Assert.Equal("hash:NewPassword123!", user.PasswordHash);
        Assert.Equal(
            "UserPasswordChanged",
            Assert.Single(harness.Audit.Actions));
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ThrowsValidationException()
    {
        var harness = CreateHarness();
        var user = CreateOperationalUser(
            UserRole.Employee,
            "wrong.current@example.com");
        harness.Users.Users.Add(user);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            harness.Service.ChangePasswordAsync(
                user.Id,
                new ChangePasswordRequest(
                    "WrongPassword123!",
                    "NewPassword123!")));
    }

    [Fact]
    public async Task ChangePasswordAsync_WithSamePassword_ThrowsValidationException()
    {
        var harness = CreateHarness();
        var user = CreateOperationalUser(
            UserRole.Employee,
            "same.password@example.com");
        harness.Users.Users.Add(user);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            harness.Service.ChangePasswordAsync(
                user.Id,
                new ChangePasswordRequest(
                    "OldPassword123!",
                    "OldPassword123!")));
    }

    private static Harness CreateHarness(
        bool departmentActive = true)
    {
        var admin = CreateOperationalUser(
            UserRole.Admin,
            "admin@example.com");
        var department = new Department("Bilgi İşlem");

        if (!departmentActive)
        {
            department.Deactivate();
        }

        var users = new FakeUserRepository();
        users.Users.Add(admin);

        var tokens = new FakeAccountTokenRepository();
        var audit = new FakeAuditLogService();
        var email = new FakeEmailSender();

        var service = new AccountLifecycleService(
            users,
            new FakeDepartmentRepository(department),
            tokens,
            new FakeTokenGenerator(),
            new FakePasswordHashService(),
            audit,
            email,
            new AccountLifecycleSettings(
                TimeSpan.FromHours(24),
                TimeSpan.FromHours(1),
                "https://frontend.example"));

        return new Harness(
            service,
            users,
            tokens,
            audit,
            email,
            admin,
            department);
    }

    private static InviteUserRequest CreateInviteRequest(
        Guid departmentId)
    {
        return new InviteUserRequest(
            "Invited User",
            "invited@example.com",
            UserRole.Technician,
            departmentId);
    }

    private static User CreateOperationalUser(
        UserRole role,
        string email)
    {
        return new User(
            "Operational User",
            email,
            "hash:OldPassword123!",
            role,
            Guid.NewGuid());
    }

    private static User CreatePendingUser(
        string email = "pending@example.com")
    {
        return User.CreateInvited(
            "Pending User",
            email,
            UserRole.Employee,
            Guid.NewGuid());
    }

    private static AccountToken AddToken(
        Harness harness,
        User user,
        string rawToken,
        AccountTokenType type)
    {
        var token = new AccountToken(
            user.Id,
            HashToken(rawToken),
            type,
            DateTime.UtcNow.AddHours(1));
        harness.Tokens.Tokens.Add(token);
        return token;
    }

    private sealed record Harness(
        AccountLifecycleService Service,
        FakeUserRepository Users,
        FakeAccountTokenRepository Tokens,
        FakeAuditLogService Audit,
        FakeEmailSender Email,
        User Admin,
        Department Department);

    private static string HashToken(string rawToken)
    {
        return Convert.ToBase64String(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(rawToken)));
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];
        public int SaveChangesCallCount { get; private set; }

        public Task<IReadOnlyList<User>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(Users);

        public Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.SingleOrDefault(user => user.Id == id));

        public Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.SingleOrDefault(user =>
                string.Equals(
                    user.Email,
                    email.Trim(),
                    StringComparison.OrdinalIgnoreCase)));

        public Task<bool> EmailExistsAsync(
            string email,
            Guid? excludedUserId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.Any(user =>
                user.Id != excludedUserId &&
                string.Equals(
                    user.Email,
                    email.Trim(),
                    StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDepartmentRepository : IDepartmentRepository
    {
        private readonly Department _department;

        public FakeDepartmentRepository(Department department)
        {
            _department = department;
        }

        public Task<IReadOnlyList<Department>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Department>>([_department]);

        public Task<Department?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Department?>(
                id == _department.Id ? _department : null);

        public Task<bool> ExistsByNameAsync(
            string name,
            Guid? excludedDepartmentId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(
            Department department,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class FakeAccountTokenRepository
        : IAccountTokenRepository
    {
        public List<AccountToken> Tokens { get; } = [];

        public Task<AccountToken?> GetByHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Tokens.SingleOrDefault(token =>
                token.TokenHash == tokenHash));

        public Task<IReadOnlyList<AccountToken>>
            GetActiveByUserAndTypeAsync(
                Guid userId,
                AccountTokenType type,
                DateTime utcNow,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AccountToken>>(
                Tokens.Where(token =>
                    token.UserId == userId &&
                    token.Type == type &&
                    token.CanBeUsed(utcNow)).ToList());

        public Task AddAsync(
            AccountToken accountToken,
            CancellationToken cancellationToken = default)
        {
            Tokens.Add(accountToken);
            return Task.CompletedTask;
        }

        public Task<bool> TryConsumeAsync(
            Guid tokenId,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            var token = Tokens.SingleOrDefault(
                accountToken => accountToken.Id == tokenId);

            if (token is null || !token.CanBeUsed(utcNow))
            {
                return Task.FromResult(false);
            }

            token.Consume(utcNow);
            return Task.FromResult(true);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeTokenGenerator : IAccountTokenGenerator
    {
        private int _count;

        public GeneratedAccountToken Generate()
        {
            _count++;
            var rawToken = $"raw-{_count}";
            return new GeneratedAccountToken(
                rawToken,
                HashToken(rawToken));
        }

        public string HashToken(string rawToken) =>
            AccountLifecycleServiceTests.HashToken(rawToken);
    }

    private sealed class FakePasswordHashService
        : IPasswordHashService
    {
        public string HashPassword(string password) =>
            $"hash:{password}";

        public PasswordVerificationOutcome VerifyPassword(
            string? passwordHash,
            string providedPassword) =>
            passwordHash == HashPassword(providedPassword)
                ? PasswordVerificationOutcome.Success
                : PasswordVerificationOutcome.Failed;
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public List<string> Actions { get; } = [];

        public Task<PagedResult<AuditLogDto>> GetPagedAsync(
            AuditLogListQuery query,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(
            Guid performedByUserId,
            string action,
            string entityName,
            string entityId,
            object? oldValues = null,
            object? newValues = null,
            CancellationToken cancellationToken = default)
        {
            Actions.Add(action);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = [];
        public Exception? ExceptionToThrow { get; set; }

        public Task SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }
    }
}
