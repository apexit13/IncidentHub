using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IncidentHub.Api.Infrastructure.Security;

// This class dynamically adds a Bearer security scheme to the OpenAPI document if
// Bearer authentication is configured in the application.
internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider
) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();

        // Only proceed if Bearer authentication is configured
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            // Define the Bearer security scheme
            var bearerScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            };

            // Ensure components are initialized
            document.Components ??= new OpenApiComponents();

            // Add the scheme to the document components
            document.AddComponent("Bearer", bearerScheme);

            // Create a security requirement referencing the scheme (use explicit list)
            var securityRequirement = new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
            };

            // Safely enumerate path items and operations (handle possible nulls)
            var pathItems = document.Paths?.Values ?? Enumerable.Empty<IOpenApiPathItem>();
            foreach (var operation in pathItems.SelectMany(p => p.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>()))
            {
                operation.Security ??= new List<OpenApiSecurityRequirement>();
                operation.Security.Add(securityRequirement);
            }
        }
    }
}
