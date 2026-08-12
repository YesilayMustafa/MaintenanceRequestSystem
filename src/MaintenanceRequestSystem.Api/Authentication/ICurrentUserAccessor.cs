using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Api.Authentication;

public interface ICurrentUserAccessor
{
    bool TryGetCurrentUser(
        out Guid userId,
        out UserRole role);
}
