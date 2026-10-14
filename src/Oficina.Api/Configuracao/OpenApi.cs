using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Oficina.Api.Configuracao;

/// <summary>Adiciona o esquema Bearer (JWT) ao documento, habilitando o botão "Authorize" do Swagger UI.</summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info.Title = "Oficina Mecânica API";
        document.Info.Description =
            "Sistema Integrado de Atendimento e Execução de Serviços. Faça login em /api/v1/auth e use o token em Authorize.";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token JWT obtido em /api/v1/auth/login ou /api/v1/auth/clientes/login."
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        return Task.CompletedTask;
    }
}
