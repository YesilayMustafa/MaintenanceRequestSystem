using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Users.Dtos;

public sealed record InviteUserRequest(
    string FullName,
    string Email,
    UserRole Role,
    Guid DepartmentId);
