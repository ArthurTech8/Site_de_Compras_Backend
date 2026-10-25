using Microsoft.AspNetCore.Authorization;
using SiteDeCompras.Domain.Usuarios;

namespace SiteDeCompras.Api.Seguranca;

/// <summary>Quem pode o quê na operação da loja.</summary>
public static class Politicas
{
    /// <summary>Ver a logística: fila de envios, painel e detalhes. O atendimento só consulta.</summary>
    public const string ConsultarLogistica = nameof(ConsultarLogistica);

    /// <summary>Mover os envios entre as etapas.</summary>
    public const string OperarLogistica = nameof(OperarLogistica);

    public static void Configurar(AuthorizationOptions o)
    {
        o.AddPolicy(ConsultarLogistica, p => p.RequireRole(
            nameof(PapelUsuario.Admin), nameof(PapelUsuario.OperadorCD), nameof(PapelUsuario.Entregador), nameof(PapelUsuario.Atendimento)));
        o.AddPolicy(OperarLogistica, p => p.RequireRole(
            nameof(PapelUsuario.Admin), nameof(PapelUsuario.OperadorCD), nameof(PapelUsuario.Entregador)));
    }
}
