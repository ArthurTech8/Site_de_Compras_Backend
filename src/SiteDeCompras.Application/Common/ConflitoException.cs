namespace SiteDeCompras.Application.Common;

/// <summary>
/// O recurso já existe (ex.: e-mail cadastrado). A API traduz para HTTP 409.
/// </summary>
public class ConflitoException(string mensagem) : Exception(mensagem);
