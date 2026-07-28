using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MaintenanceRequestSystem.Api.OpenApi;

public sealed class AuthOperationTransformer
    : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var endpointMetadata =
            context.Description
                .ActionDescriptor
                .EndpointMetadata;

        var allowsAnonymous =
            endpointMetadata
                .OfType<IAllowAnonymous>()
                .Any();

        var requiresAuthorization =
            endpointMetadata
                .OfType<IAuthorizeData>()
                .Any();

        if (allowsAnonymous ||
            !requiresAuthorization ||
            context.Document is null)
        {
            return Task.CompletedTask;
        }

        operation.Security ??=
            new List<OpenApiSecurityRequirement>();

        operation.Security.Add(
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecuritySchemeReference(
                        "Bearer",
                        context.Document)
                ] = new List<string>()
            });

        return Task.CompletedTask;
    }
}