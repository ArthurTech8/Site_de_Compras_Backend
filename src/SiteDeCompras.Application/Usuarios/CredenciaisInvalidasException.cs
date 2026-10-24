namespace SiteDeCompras.Application.Usuarios;

/// <summary>
/// Login recusado. A mensagem é a mesma para e-mail inexistente, senha errada e conta bloqueada,
/// para não revelar quais e-mails têm conta. A API traduz para HTTP 401.
/// </summary>
public class CredenciaisInvalidasException()
    : Exception("E-mail ou senha inválidos. Depois de várias tentativas a conta fica bloqueada por alguns minutos.");
