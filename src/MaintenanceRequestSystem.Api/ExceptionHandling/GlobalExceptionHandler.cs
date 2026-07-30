using MaintenanceRequestSystem.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequestSystem.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var error = exception switch
        {
            RequestValidationException => new ErrorDetails(
                StatusCodes.Status400BadRequest,
                "Geçersiz istek",
                exception.Message),

            ArgumentException => new ErrorDetails(
                StatusCodes.Status400BadRequest,
                "Geçersiz istek",
                exception.Message),

            KeyNotFoundException => new ErrorDetails(
                StatusCodes.Status404NotFound,
                "Kayıt bulunamadı",
                exception.Message),

            ConflictException => new ErrorDetails(
                StatusCodes.Status409Conflict,
                "İşlem çakışması",
                exception.Message),

            ForbiddenException => new ErrorDetails(
                StatusCodes.Status403Forbidden,
                "Erişim reddedildi",
                exception.Message),

            InvalidCredentialsException => new ErrorDetails(
                StatusCodes.Status401Unauthorized,
                "Kimlik doğrulama başarısız",
                exception.Message),

            _ => new ErrorDetails(
                StatusCodes.Status500InternalServerError,
                "Sunucu hatası",
                "Beklenmeyen bir hata oluştu.")


        };

        if (error.StatusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Beklenmeyen bir hata oluştu. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "İstek işlenirken kontrollü bir hata oluştu. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = error.StatusCode;

        var problemDetails = new ProblemDetails
        {
            Status = error.StatusCode,
            Title = error.Title,
            Detail = error.Detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] =
            httpContext.TraceIdentifier;

        return await _problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            });
    }

    private sealed record ErrorDetails(
        int StatusCode,
        string Title,
        string Detail);
}