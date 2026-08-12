using System.IdentityModel.Tokens.Jwt;
using MaintenanceRequestSystem.Application.Authentication;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Api.Authentication;

public sealed class CurrentUserAccessor
    : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool TryGetCurrentUser(
        out Guid userId,
        out UserRole role)
    {
        userId = Guid.Empty;
        role = default;

        var principal =
            _httpContextAccessor.HttpContext?.User;

        var userIdValue = principal?.FindFirst(
            JwtRegisteredClaimNames.Sub)?.Value;

        var roleValue = principal?.FindFirst(
            AuthenticationClaimNames.Role)?.Value;

        if (!Guid.TryParse(userIdValue, out var parsedUserId) ||
            !Enum.TryParse<UserRole>(roleValue, out var parsedRole) ||
            !Enum.IsDefined(parsedRole))
        {
            return false;
        }

        userId = parsedUserId;
        role = parsedRole;

        return true;
    }
}
