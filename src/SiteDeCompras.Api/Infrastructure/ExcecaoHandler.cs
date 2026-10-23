using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Usuarios;
using SiteDeCompras.Domain.Common;

namespace SiteDeCompras.Api.Infrastructure;

/// <summary>
/// Converte exceções de negócio em respostas ProblemDetails (RFC 9457).
/// </summary>
public class ExcecaoHandler(IProblemDetailsService problemDetails, ILogger<ExcecaoHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is ValidacaoException validacao)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = exception,
                ProblemDetails = new ValidationProblemDetails(
                    new Dictionary<string, string[]> { [validacao.Campo] = [validacao.Message] })
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Dados inválidos",
                },
            });
        }

        var (status, titulo) = exception switch
        {
            CredenciaisInvalidasException => (StatusCodes.Status401Unauthorized, "Não foi possível entrar"),
            ConflitoException => (StatusCodes.Status409Conflict, "Conflito"),
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno"),
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Erro não tratado em {Path}", context.Request.Path);

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = titulo,
                // Mensagens internas não vazam para o cliente.
                Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message,
            },
        });
    }
}
