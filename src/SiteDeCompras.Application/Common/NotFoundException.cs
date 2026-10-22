namespace SiteDeCompras.Application.Common;

/// <summary>
/// Recurso inexistente. A API traduz para HTTP 404.
/// </summary>
public class NotFoundException(string recurso, object id)
    : Exception($"{recurso} '{id}' não encontrado.");
