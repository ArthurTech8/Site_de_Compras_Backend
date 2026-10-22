using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SiteDeCompras.Application.Common;
using SiteDeCompras.Domain.Common;

namespace SiteDeCompras.Api.Infrastructure;

/// <summary>
/// Converte exceções de negócio em respostas ProblemDetails (RFC 9457).
/// </summary>
public class ExcecaoHandler(IProblemDetailsService problemDetails, ILogger<ExcecaoHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, titulo) = exception switch
        {
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
