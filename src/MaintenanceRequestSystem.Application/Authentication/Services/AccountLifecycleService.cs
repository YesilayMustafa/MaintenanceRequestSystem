using System.Net;
using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.Authentication.Models;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Departments.Interfaces;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Authentication.Services;

public sealed class AccountLifecycleService
    : IAccountLifecycleService
{
    public const string GenericForgotPasswordMessage =
        "Eğer bu e-posta adresiyle kullanılabilir bir hesap varsa, " +
        "şifre sıfırlama talimatları gönderildi.";

    private const string InvalidTokenMessage =
        "Token geçersiz veya artık kullanılamıyor.";

    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAccountTokenRepository _accountTokenRepository;
    private readonly IAccountTokenGenerator _accountTokenGenerator;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailSender _emailSender;
    private readonly AccountLifecycleSettings _settings;

    public AccountLifecycleService(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IAccountTokenRepository accountTokenRepository,
        IAccountTokenGenerator accountTokenGenerator,
        IPasswordHashService passwordHashService,
        IAuditLogService auditLogService,
        IEmailSender emailSender,
        AccountLifecycleSettings settings)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _accountTokenRepository = accountTokenRepository;
        _accountTokenGenerator = accountTokenGenerator;
        _passwordHashService = passwordHashService;
        _auditLogService = auditLogService;
        _emailSender = emailSender;
        _settings = settings;

        EnsureValidSettings(settings);
    }

    public async Task<UserDto> InviteUserAsync(
        Guid performedByUserId,
        InviteUserRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(performedByUserId);
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.IsDefined(request.Role))
        {
            throw new RequestValidationException(
                "Geçersiz kullanıcı rolü.");
        }

        User? invitedUser = null;
        string? departmentName = null;
        string? rawToken = null;
        var expiresAt = default(DateTime);

        await _userRepository.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                await EnsureOperationalAdminAsync(
                    performedByUserId,
                    transactionCancellationToken);

                var emailExists =
                    await _userRepository.EmailExistsAsync(
                        request.Email,
                        cancellationToken:
                            transactionCancellationToken);

                if (emailExists)
                {
                    throw new ConflictException(
                        "Bu e-posta adresiyle kayıtlı bir kullanıcı zaten var.");
                }

                var department =
                    await _departmentRepository.GetByIdAsync(
                        request.DepartmentId,
                        transactionCancellationToken);

                if (department is null)
                {
                    throw new KeyNotFoundException(
                        "Seçilen departman bulunamadı.");
                }

                if (!department.IsActive)
                {
                    throw new RequestValidationException(
                        "Pasif bir departmana kullanıcı atanamaz.");
                }

                var user = User.CreateInvited(
                    request.FullName,
                    request.Email,
                    request.Role,
                    request.DepartmentId);

                var generatedToken =
                    _accountTokenGenerator.Generate();

                var utcNow = DateTime.UtcNow;
                var tokenExpiresAt =
                    utcNow.Add(_settings.InvitationLifetime);

                var accountToken = new AccountToken(
                    user.Id,
                    generatedToken.TokenHash,
                    AccountTokenType.Invitation,
                    tokenExpiresAt);

                await _userRepository.AddAsync(
                    user,
                    transactionCancellationToken);

                await _accountTokenRepository.AddAsync(
                    accountToken,
                    transactionCancellationToken);

                await _auditLogService.AddAsync(
                    performedByUserId,
                    "UserInvited",
                    nameof(User),
                    user.Id.ToString(),
                    newValues: new
                    {
                        user.Email,
                        user.Role,
                        AccountStatus = user.AccountStatus.ToString()
                    },
                    cancellationToken:
                        transactionCancellationToken);

                await _userRepository.SaveChangesAsync(
                    transactionCancellationToken);

                invitedUser = user;
                departmentName = department.Name;
                rawToken = generatedToken.RawToken;
                expiresAt = tokenExpiresAt;
            },
            cancellationToken);

        try
        {
            await _emailSender.SendAsync(
                CreateInvitationEmail(
                    invitedUser!,
                    rawToken!,
                    expiresAt),
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new EmailDeliveryException(
                "Kullanıcı oluşturuldu ancak davet e-postası gönderilemedi. " +
                "Davet yeniden gönderilebilir.",
                exception);
        }

        return MapToDto(invitedUser!, departmentName!);
    }

    public async Task ResendInvitationAsync(
        Guid userId,
        Guid performedByUserId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(userId);
        EnsureValidId(performedByUserId);

        User? invitedUser = null;
        string? rawToken = null;
        var expiresAt = default(DateTime);

        await _userRepository.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                await EnsureOperationalAdminAsync(
                    performedByUserId,
                    transactionCancellationToken);

                var user = await _userRepository.GetByIdAsync(
                    userId,
                    transactionCancellationToken);

                if (user is null)
                {
                    throw new KeyNotFoundException(
                        "Kullanıcı bulunamadı.");
                }

                if (user.AccountStatus !=
                    AccountStatus.PendingInvitation)
                {
                    throw new RequestValidationException(
                        "Davet yalnızca bekleyen aktif kullanıcılar için yeniden gönderilebilir.");
                }

                var utcNow = DateTime.UtcNow;
                await RevokeActiveTokensAsync(
                    user.Id,
                    AccountTokenType.Invitation,
                    utcNow,
                    excludedTokenId: null,
                    transactionCancellationToken);

                var generatedToken =
                    _accountTokenGenerator.Generate();

                var tokenExpiresAt =
                    utcNow.Add(_settings.InvitationLifetime);

                await _accountTokenRepository.AddAsync(
                    new AccountToken(
                        user.Id,
                        generatedToken.TokenHash,
                        AccountTokenType.Invitation,
                        tokenExpiresAt),
                    transactionCancellationToken);

                await _auditLogService.AddAsync(
                    performedByUserId,
                    "UserInvitationResent",
                    nameof(User),
                    user.Id.ToString(),
                    newValues: new
                    {
                        AccountStatus = user.AccountStatus.ToString()
                    },
                    cancellationToken:
                        transactionCancellationToken);

                await _userRepository.SaveChangesAsync(
                    transactionCancellationToken);

                invitedUser = user;
                rawToken = generatedToken.RawToken;
                expiresAt = tokenExpiresAt;
            },
            cancellationToken);

        try
        {
            await _emailSender.SendAsync(
                CreateInvitationEmail(
                    invitedUser!,
                    rawToken!,
                    expiresAt),
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new EmailDeliveryException(
                "Yeni davet oluşturuldu ancak e-posta gönderilemedi. " +
                "Davet yeniden gönderilebilir.",
                exception);
        }
    }

    public Task AcceptInvitationAsync(
        AcceptInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureTokenProvided(request.Token);

        return _userRepository.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                var utcNow = DateTime.UtcNow;
                var token = await GetUsableTokenAsync(
                    request.Token,
                    AccountTokenType.Invitation,
                    utcNow,
                    transactionCancellationToken);

                var user = await _userRepository.GetByIdAsync(
                    token.UserId,
                    transactionCancellationToken);

                if (user is null ||
                    !user.IsActive ||
                    user.AccountStatus !=
                        AccountStatus.PendingInvitation)
                {
                    throw InvalidToken();
                }

                PasswordPolicy.EnsureValid(request.NewPassword);

                var passwordHash =
                    _passwordHashService.HashPassword(
                        request.NewPassword);

                if (!await _accountTokenRepository.TryConsumeAsync(
                        token.Id,
                        utcNow,
                        transactionCancellationToken))
                {
                    throw new ConflictException(
                        "Token başka bir istek tarafından kullanıldı.");
                }

                user.AcceptInvitation(passwordHash);

                await RevokeActiveTokensAsync(
                    user.Id,
                    AccountTokenType.Invitation,
                    utcNow,
                    token.Id,
                    transactionCancellationToken);

                await _auditLogService.AddAsync(
                    user.Id,
                    "UserInvitationAccepted",
                    nameof(User),
                    user.Id.ToString(),
                    new
                    {
                        AccountStatus =
                            AccountStatus.PendingInvitation.ToString()
                    },
                    new
                    {
                        AccountStatus =
                            AccountStatus.Active.ToString()
                    },
                    transactionCancellationToken);

                await _userRepository.SaveChangesAsync(
                    transactionCancellationToken);
            },
            cancellationToken);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? rawToken = null;
        User? userToNotify = null;
        var expiresAt = default(DateTime);

        await _userRepository.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return;
                }

                var user = await _userRepository.GetByEmailAsync(
                    request.Email,
                    transactionCancellationToken);

                if (user is null || !user.IsOperational)
                {
                    return;
                }

                var utcNow = DateTime.UtcNow;
                await RevokeActiveTokensAsync(
                    user.Id,
                    AccountTokenType.PasswordReset,
                    utcNow,
                    excludedTokenId: null,
                    transactionCancellationToken);

                var generatedToken =
                    _accountTokenGenerator.Generate();

                var tokenExpiresAt =
                    utcNow.Add(_settings.PasswordResetLifetime);

                await _accountTokenRepository.AddAsync(
                    new AccountToken(
                        user.Id,
                        generatedToken.TokenHash,
                        AccountTokenType.PasswordReset,
                        tokenExpiresAt),
                    transactionCancellationToken);

                await _userRepository.SaveChangesAsync(
                    transactionCancellationToken);

                rawToken = generatedToken.RawToken;
                userToNotify = user;
                expiresAt = tokenExpiresAt;
            },
            cancellationToken);

        if (userToNotify is not null)
        {
            try
            {
                await _emailSender.SendAsync(
                    CreatePasswordResetEmail(
                        userToNotify,
                        rawToken!,
                        expiresAt),
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // Anonymous yanıt, hesap varlığını veya teslimat sonucunu sızdırmaz.
            }
        }

        return new ForgotPasswordResponse(
            GenericForgotPasswordMessage);
    }

    public Task ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureTokenProvided(request.Token);

        return _userRepository.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                var utcNow = DateTime.UtcNow;
                var token = await GetUsableTokenAsync(
                    request.Token,
                    AccountTokenType.PasswordReset,
                    utcNow,
                    transactionCancellationToken);

                var user = await _userRepository.GetByIdAsync(
                    token.UserId,
                    transactionCancellationToken);

                if (user is null || !user.IsOperational)
                {
                    throw InvalidToken();
                }

                PasswordPolicy.EnsureValid(request.NewPassword);

                var passwordHash =
                    _passwordHashService.HashPassword(
                        request.NewPassword);

                if (!await _accountTokenRepository.TryConsumeAsync(
                        token.Id,
                        utcNow,
                        transactionCancellationToken))
                {
                    throw new ConflictException(
                        "Token başka bir istek tarafından kullanıldı.");
                }

                user.ChangePasswordHash(passwordHash);

                await RevokeActiveTokensAsync(
                    user.Id,
                    AccountTokenType.PasswordReset,
                    utcNow,
                    token.Id,
                    transactionCancellationToken);

                await _auditLogService.AddAsync(
                    user.Id,
                    "UserPasswordReset",
                    nameof(User),
                    user.Id.ToString(),
                    newValues: new
                    {
                        SecurityVersionChanged = true
                    },
                    cancellationToken:
                        transactionCancellationToken);

                await _userRepository.SaveChangesAsync(
                    transactionCancellationToken);
            },
            cancellationToken);
    }

    public Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(userId);
        ArgumentNullException.ThrowIfNull(request);

        return _userRepository.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                var user = await _userRepository.GetByIdAsync(
                    userId,
                    transactionCancellationToken);

                if (user is null || !user.IsOperational)
                {
                    throw new InvalidCredentialsException(
                        "Kimlik doğrulama bilgileri geçersiz.");
                }

                var currentPasswordResult =
                    _passwordHashService.VerifyPassword(
                        user.PasswordHash,
                        request.CurrentPassword);

                if (!currentPasswordResult.Succeeded)
                {
                    throw new RequestValidationException(
                        "Mevcut parola hatalı.");
                }

                PasswordPolicy.EnsureValid(request.NewPassword);

                var samePasswordResult =
                    _passwordHashService.VerifyPassword(
                        user.PasswordHash,
                        request.NewPassword);

                if (samePasswordResult.Succeeded)
                {
                    throw new RequestValidationException(
                        "Yeni parola mevcut paroladan farklı olmalıdır.");
                }

                var passwordHash =
                    _passwordHashService.HashPassword(
                        request.NewPassword);

                user.ChangePasswordHash(passwordHash);

                await _auditLogService.AddAsync(
                    user.Id,
                    "UserPasswordChanged",
                    nameof(User),
                    user.Id.ToString(),
                    newValues: new
                    {
                        SecurityVersionChanged = true
                    },
                    cancellationToken:
                        transactionCancellationToken);

                await _userRepository.SaveChangesAsync(
                    transactionCancellationToken);
            },
            cancellationToken);
    }

    private async Task<AccountToken> GetUsableTokenAsync(
        string rawToken,
        AccountTokenType expectedType,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var tokenHash =
            _accountTokenGenerator.HashToken(rawToken);

        var token = await _accountTokenRepository.GetByHashAsync(
            tokenHash,
            cancellationToken);

        if (token is null ||
            token.Type != expectedType ||
            !token.CanBeUsed(utcNow))
        {
            throw InvalidToken();
        }

        return token;
    }

    private async Task RevokeActiveTokensAsync(
        Guid userId,
        AccountTokenType type,
        DateTime utcNow,
        Guid? excludedTokenId,
        CancellationToken cancellationToken)
    {
        var tokens =
            await _accountTokenRepository
                .GetActiveByUserAndTypeAsync(
                    userId,
                    type,
                    utcNow,
                    cancellationToken);

        foreach (var token in tokens)
        {
            if (token.Id != excludedTokenId)
            {
                token.Revoke(utcNow);
            }
        }
    }

    private async Task EnsureOperationalAdminAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(
            userId,
            cancellationToken);

        if (user is null ||
            !user.IsOperational ||
            user.Role != UserRole.Admin)
        {
            throw new ForbiddenException(
                "Bu işlemi yalnızca aktif yöneticiler yapabilir.");
        }
    }

    private EmailMessage CreateInvitationEmail(
        User user,
        string rawToken,
        DateTime expiresAt)
    {
        var link = BuildFrontendLink(
            "accept-invitation",
            rawToken);

        var encodedName = WebUtility.HtmlEncode(user.FullName);
        var encodedLink = WebUtility.HtmlEncode(link);
        var expiration = expiresAt.ToString("u");

        return new EmailMessage(
            user.Email,
            "Bakım Talep Sistemi hesap daveti",
            $"Merhaba {user.FullName},{Environment.NewLine}{Environment.NewLine}" +
            "Bakım Talep Sistemi hesabınızı oluşturmak için aşağıdaki bağlantıyı kullanın:" +
            $"{Environment.NewLine}{link}{Environment.NewLine}{Environment.NewLine}" +
            $"Bu davet {expiration} tarihine kadar geçerlidir.",
            $"<p>Merhaba {encodedName},</p>" +
            "<p>Bakım Talep Sistemi hesabınızı oluşturmak için aşağıdaki bağlantıyı kullanın.</p>" +
            $"<p><a href=\"{encodedLink}\">Hesabımı Oluştur</a></p>" +
            $"<p>Bu davet {WebUtility.HtmlEncode(expiration)} tarihine kadar geçerlidir.</p>");
    }

    private EmailMessage CreatePasswordResetEmail(
        User user,
        string rawToken,
        DateTime expiresAt)
    {
        var link = BuildFrontendLink(
            "reset-password",
            rawToken);

        var encodedLink = WebUtility.HtmlEncode(link);
        var expiration = expiresAt.ToString("u");

        return new EmailMessage(
            user.Email,
            "Bakım Talep Sistemi şifre sıfırlama",
            "Şifrenizi sıfırlamak için aşağıdaki bağlantıyı kullanın:" +
            $"{Environment.NewLine}{link}{Environment.NewLine}{Environment.NewLine}" +
            $"Bağlantı {expiration} tarihine kadar geçerlidir.{Environment.NewLine}" +
            "Bu talebi siz yapmadıysanız bu e-postayı yok sayabilirsiniz.",
            "<p>Şifrenizi sıfırlamak için aşağıdaki bağlantıyı kullanın.</p>" +
            $"<p><a href=\"{encodedLink}\">Şifremi Sıfırla</a></p>" +
            $"<p>Bağlantı {WebUtility.HtmlEncode(expiration)} tarihine kadar geçerlidir.</p>" +
            "<p>Bu talebi siz yapmadıysanız bu e-postayı yok sayabilirsiniz.</p>");
    }

    private string BuildFrontendLink(
        string path,
        string rawToken)
    {
        return $"{_settings.FrontendBaseUrl.TrimEnd('/')}/" +
            $"{path}?token={Uri.EscapeDataString(rawToken)}";
    }

    private static UserDto MapToDto(
        User user,
        string departmentName)
    {
        return new UserDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            user.DepartmentId,
            departmentName,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
            user.AccountStatus.ToString());
    }

    private static void EnsureValidId(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new RequestValidationException(
                "Geçerli bir kullanıcı kimliği gereklidir.");
        }
    }

    private static void EnsureTokenProvided(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw InvalidToken();
        }
    }

    private static RequestValidationException InvalidToken()
    {
        return new RequestValidationException(
            InvalidTokenMessage);
    }

    private static void EnsureValidSettings(
        AccountLifecycleSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.InvitationLifetime <= TimeSpan.Zero ||
            settings.PasswordResetLifetime <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Hesap token geçerlilik süreleri pozitif olmalıdır.");
        }

        if (!Uri.TryCreate(
                settings.FrontendBaseUrl,
                UriKind.Absolute,
                out var frontendUri) ||
            frontendUri.Scheme is not ("http" or "https") ||
            !string.IsNullOrEmpty(frontendUri.Query) ||
            !string.IsNullOrEmpty(frontendUri.Fragment))
        {
            throw new InvalidOperationException(
                "Frontend BaseUrl geçerli bir HTTP(S) adresi olmalıdır.");
        }
    }
}
