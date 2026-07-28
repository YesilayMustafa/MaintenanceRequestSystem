using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Users.Interfaces;

namespace MaintenanceRequestSystem.Application.Authentication.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHashService passwordHashService,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHashService = passwordHashService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new RequestValidationException(
                "E-posta ve parola alanları zorunludur.");
        }

        var normalizedEmail =
            request.Email.Trim().ToLowerInvariant();

        var user =
            await _userRepository.GetByEmailAsync(
                normalizedEmail,
                cancellationToken);

        if (user is null ||
            !user.IsActive ||
            !_passwordHashService.VerifyPassword(
                user.PasswordHash,
                request.Password))
        {
            throw new InvalidCredentialsException(
                "E-posta veya parola hatalı.");
        }

        var token =
            _jwtTokenService.CreateToken(user);

        var userDto =
            new AuthenticatedUserDto(
                user.Id,
                user.FullName,
                user.Email,
                user.Role.ToString());

        return new LoginResponse(
            token.AccessToken,
            token.ExpiresAt,
            userDto);
    }
}