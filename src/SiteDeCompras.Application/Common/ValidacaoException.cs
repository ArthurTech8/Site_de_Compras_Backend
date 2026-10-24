namespace SiteDeCompras.Application.Common;

/// <summary>
/// Dado de entrada inválido que só a aplicação consegue verificar (ex.: senha fraca).
/// A API traduz para HTTP 400 com o erro no campo.
/// </summary>
public class ValidacaoException(string campo, string mensagem) : Exception(mensagem)
{
    public string Campo { get; } = campo;
}
