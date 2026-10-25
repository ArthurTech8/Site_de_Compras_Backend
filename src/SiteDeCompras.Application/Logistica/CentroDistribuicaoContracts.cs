using System.ComponentModel.DataAnnotations;
using SiteDeCompras.Application.Enderecos;
using SiteDeCompras.Domain.Logistica;

namespace SiteDeCompras.Application.Logistica;

// ---------- Estoque ----------

/// <summary>Um produto na prateleira: onde está, quanto há para vender e quanto já foi vendido e espera a separação.</summary>
public record PosicaoEstoqueDto(
    int ProdutoId,
    string Sku,
    string Nome,
    string? ImagemUrl,
    string? Local,
    int Disponivel,
    int Reservado,
    int Divergencia,
    DateTime? DivergenciaDesde);

public record MoverProdutoRequest
{
    /// <summary>Corredor-módulo-nível, como A-03-2. Vazio tira o endereço.</summary>
    [StringLength(8)]
    public string? Local { get; init; }
}

public record InventarioRequest
{
    /// <summary>Unidades contadas na prateleira, incluindo as já vendidas que ainda não foram separadas.</summary>
    [Range(0, 1_000_000)]
    public int QuantidadeContada { get; init; }
}

public record InventarioDto(PosicaoEstoqueDto Posicao, int Ajuste, string Mensagem);

// ---------- Separação ----------

public record GerarOndaRequest
{
    [Range(1, OndaSeparacao.TamanhoMaximo)]
    public int Envios { get; init; } = OndaSeparacao.TamanhoPadrao;
}

public record OndaResumoDto(int Id, StatusOnda Status, DateTime CriadaEm, DateTime? ConcluidaEm, int Envios, int Unidades, int Separadas);

/// <summary>Uma parada da rota de coleta: o produto, a prateleira e quantas unidades levar.</summary>
public record ItemColetaDto(int ProdutoId, string Sku, string Nome, string? ImagemUrl, string? Local, int Quantidade, int Separadas);

public record EnvioDaOndaDto(Guid Id, Guid PedidoId, StatusEnvio Status, string Destinatario, int Unidades, int Separadas);

public record OndaDto(
    int Id,
    StatusOnda Status,
    DateTime CriadaEm,
    DateTime? ConcluidaEm,
    IReadOnlyList<ItemColetaDto> Coleta,
    IReadOnlyList<EnvioDaOndaDto> Envios);

public record BipagemRequest
{
    /// <summary>O que o leitor leu: SKU do produto ou código da etiqueta.</summary>
    [Required, StringLength(40, MinimumLength = 1)]
    public string Codigo { get; init; } = string.Empty;
}

public record FaltaRequest
{
    [Range(1, int.MaxValue)]
    public int ProdutoId { get; init; }
}

/// <summary>A onda atualizada e uma frase para o separador, como "Caneca: 2 de 3".</summary>
public record ResultadoOndaDto(OndaDto Onda, string Mensagem);

// ---------- Embalagem ----------

public record CaixaDto(string Codigo, string Nome, int AlturaCm, int LarguraCm, int ComprimentoCm, bool Cabe, int PesoEsperadoGramas);

public record ItemConferenciaDto(int ProdutoId, string Sku, string Nome, string? ImagemUrl, int Quantidade, int Conferidas);

public record PacoteDto(string Caixa, int PesoGramas, int AlturaCm, int LarguraCm, int ComprimentoCm)
{
    public static PacoteDto? De(Pacote? p) => p is null ? null : new(p.Caixa, p.PesoGramas, p.AlturaCm, p.LarguraCm, p.ComprimentoCm);
}

/// <summary>Tudo o que a bancada de embalagem mostra para um envio.</summary>
public record EmbalagemDto(
    Guid EnvioId,
    Guid PedidoId,
    StatusEnvio Status,
    string Destinatario,
    string Cidade,
    string Uf,
    IReadOnlyList<ItemConferenciaDto> Itens,
    string CaixaSugerida,
    IReadOnlyList<CaixaDto> Caixas,
    PacoteDto? Pacote,
    string? CodigoRastreio);

public record EmbalarRequest
{
    [Required, StringLength(10)]
    public string Caixa { get; init; } = string.Empty;

    /// <summary>O peso da balança, em gramas.</summary>
    [Range(1, 300_000)]
    public int PesoGramas { get; init; }
}

public record RemetenteDto(string Nome, string Linha1, string Linha2);

/// <summary>A etiqueta 10×15 cm colada no pacote.</summary>
public record EtiquetaDto(
    Guid EnvioId,
    Guid PedidoId,
    string CodigoRastreio,
    string Transportadora,
    RemetenteDto Remetente,
    EnderecoDto Destino,
    PacoteDto? Pacote,
    int Unidades,
    DateTime GeradaEm,
    DateTime PrazoDespacho,
    DateTime PrevisaoEntrega);

// ---------- Expedição ----------

public record ManifestoResumoDto(
    int Id,
    string Transportadora,
    StatusManifesto Status,
    DateTime CriadoEm,
    DateTime? FechadoEm,
    DateTime? ColetadoEm,
    int Volumes,
    int PesoGramas);

public record PacoteManifestoDto(Guid EnvioId, Guid PedidoId, string CodigoRastreio, string Destinatario, string Cidade, string Uf, string? Caixa, int? PesoGramas);

public record ManifestoDto(
    int Id,
    string Transportadora,
    StatusManifesto Status,
    DateTime CriadoEm,
    DateTime? FechadoEm,
    DateTime? ColetadoEm,
    IReadOnlyList<PacoteManifestoDto> Pacotes,
    int ProntosForaDoManifesto);

public record ExpedicaoDto(IReadOnlyList<ManifestoResumoDto> Manifestos, int ProntosSemManifesto);
