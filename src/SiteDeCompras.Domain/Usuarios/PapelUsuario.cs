namespace SiteDeCompras.Domain.Usuarios;

public enum PapelUsuario
{
    Cliente,
    Admin,

    /// <summary>Separa, embala e expede os pedidos no centro de distribuição.</summary>
    OperadorCD,

    /// <summary>Faz a última milha: sai para entrega e confirma a entrega.</summary>
    Entregador,

    /// <summary>Consulta pedidos e envios para responder clientes, sem alterar nada.</summary>
    Atendimento
}
