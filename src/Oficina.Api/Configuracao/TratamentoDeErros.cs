using Microsoft.AspNetCore.Diagnostics;
using Oficina.Application.Comum;
using Oficina.Domain.Comum;

namespace Oficina.Api.Configuracao;

/// <summary>Converte exceções em ProblemDetails (RFC 9457), sem stack trace na resposta.</summary>
internal sealed class TratamentoDeErros(IProblemDetailsService problemDetails, ILogger<TratamentoDeErros> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, titulo) = exception switch
        {
            DadoInvalidoException => (StatusCodes.Status400BadRequest, "Dados inválidos"),
            BadHttpRequestException e => (e.StatusCode, "Requisição inválida"),
            CredenciaisInvalidasException => (StatusCodes.Status401Unauthorized, "Não autenticado"),
            NaoEncontradoException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ConflitoException or TransicaoDeStatusInvalidaException => (StatusCodes.Status409Conflict, "Conflito"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Erro não tratado");

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = status,
                Title = titulo,
                Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message
            }
        });
    }
}
