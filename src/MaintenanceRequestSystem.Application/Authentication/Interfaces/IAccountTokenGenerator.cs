using MaintenanceRequestSystem.Application.Authentication.Models;

namespace MaintenanceRequestSystem.Application.Authentication.Interfaces;

public interface IAccountTokenGenerator
{
    GeneratedAccountToken Generate();

    string HashToken(string rawToken);
}
