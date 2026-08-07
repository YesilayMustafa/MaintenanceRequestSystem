using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

internal static class TicketServiceGuards
{
    internal static void EnsureValidId(
        Guid id,
        string errorMessage)
    {
        if (id == Guid.Empty)
        {
            throw new RequestValidationException(
                errorMessage);
        }
    }

    internal static void EnsureSupportedRole(
        UserRole role)
    {
        if (!Enum.IsDefined(
                typeof(UserRole),
                role))
        {
            throw new ForbiddenException(
                "Desteklenmeyen kullanıcı rolü.");
        }
    }
}
