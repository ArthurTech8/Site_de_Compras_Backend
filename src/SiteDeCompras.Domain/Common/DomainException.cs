namespace SiteDeCompras.Domain.Common;

/// <summary>
/// Violação de uma regra de negócio. A API traduz para HTTP 422.
/// </summary>
public class DomainException(string message) : Exception(message);
