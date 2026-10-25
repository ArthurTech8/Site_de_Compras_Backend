namespace SiteDeCompras.Domain.Pedidos;

/// <summary>
/// O que o cliente vê. O detalhe de cada etapa fica no envio; o pedido só acompanha.
/// </summary>
public enum StatusPedido
{
    /// <summary>Pagamento aprovado. Hoje o pagamento é simulado e sempre aprovado.</summary>
    Pago,

    /// <summary>No centro de distribuição: separação, embalagem ou aguardando coleta.</summary>
    EmPreparacao,

    /// <summary>Com a transportadora.</summary>
    Enviado,

    Entregue
}
