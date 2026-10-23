using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SiteDeCompras.Application.Carrinhos;
using SiteDeCompras.Application.Pagamentos;
using SiteDeCompras.Application.Pedidos;
using SiteDeCompras.Application.Produtos;
using SiteDeCompras.Domain.Pagamentos;

namespace SiteDeCompras.Tests;

public class PagamentoApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private async Task<Guid> CarrinhoCom(params (int produtoId, int quantidade)[] itens)
    {
        var criar = await _client.PostAsync("/api/v1/carrinhos", null);
        var carrinho = (await criar.Content.ReadFromJsonAsync<CarrinhoDto>())!;
        foreach (var (produtoId, quantidade) in itens)
        {
            var resposta = await _client.PostAsJsonAsync($"/api/v1/carrinhos/{carrinho.Id}/itens",
                new AdicionarItemRequest { ProdutoId = produtoId, Quantidade = quantidade });
            resposta.EnsureSuccessStatusCode();
        }
        return carrinho.Id;
    }

    private Task<HttpResponseMessage> Fechar(Guid carrinhoId, FormaPagamento forma, int? parcelas = null) =>
        _client.PostAsJsonAsync("/api/v1/pedidos",
            new FecharPedidoRequest { CarrinhoId = carrinhoId, FormaPagamento = forma, Parcelas = parcelas }, Json);

    [Fact]
    public async Task Opcoes_usam_o_total_do_carrinho_e_o_maior_desconto_pix()
    {
        // Caneca 2 x 59,00 (Pix 5%) + Geladeira 2300,00 (Pix 10%) = 2418,00
        var id = await CarrinhoCom((5, 2), (1, 1));

        var opcoes = await _client.GetFromJsonAsync<OpcoesPagamentoDto>($"/api/v1/carrinhos/{id}/pagamento", Json);

        Assert.NotNull(opcoes);
        Assert.Equal(2418.00m, opcoes.Subtotal);
        Assert.Equal(10m, opcoes.Pix.DescontoPercentual);
        Assert.Equal("Geladeira", opcoes.Pix.ProdutoDoDesconto?.NomeProduto);
        Assert.Equal(241.80m, opcoes.Pix.Desconto);
        Assert.Equal(2176.20m, opcoes.Pix.Total);
        Assert.Equal([5m, 10m], opcoes.Pix.DescontosPorProduto.Select(d => d.DescontoPercentual));
        Assert.Equal(2418.00m, opcoes.Debito.Total);
        Assert.Equal(0.0299m, opcoes.Credito.TaxaJurosMensal);
        Assert.Equal(Enumerable.Range(6, 11), opcoes.Credito.Parcelamentos.Select(p => p.Parcelas));
        var seis = opcoes.Credito.Parcelamentos[0];
        Assert.Equal(446.21m, seis.ValorParcela);
        Assert.Equal(2677.26m, seis.Total);
        Assert.Equal(259.26m, seis.Juros);
    }

    [Fact]
    public async Task Opcoes_de_carrinho_vazio_retornam_422()
    {
        var id = await CarrinhoCom();

        var resposta = await _client.GetAsync($"/api/v1/carrinhos/{id}/pagamento");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Fechar_no_credito_grava_o_pedido_baixa_estoque_e_esvazia_o_carrinho()
    {
        var estoqueAntes = (await _client.GetFromJsonAsync<ProdutoDto>("/api/v1/produtos/3"))!.Estoque;
        var id = await CarrinhoCom((3, 1)); // Celular Xiaomi 2189,90

        var resposta = await Fechar(id, FormaPagamento.Credito, 12);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var pedido = (await resposta.Content.ReadFromJsonAsync<PedidoDto>(Json))!;
        Assert.Equal(FormaPagamento.Credito, pedido.Pagamento.Forma);
        Assert.Equal(12, pedido.Pagamento.Parcelas);
        Assert.Equal(pedido.Pagamento.ValorParcela * 12, pedido.Pagamento.Total);
        Assert.True(pedido.Pagamento.Juros > 0);

        var salvo = await _client.GetFromJsonAsync<PedidoDto>($"/api/v1/pedidos/{pedido.Id}", Json);
        Assert.Equal(pedido.Pagamento.Total, salvo!.Pagamento.Total);
        Assert.Empty((await _client.GetFromJsonAsync<CarrinhoDto>($"/api/v1/carrinhos/{id}"))!.Itens);
        Assert.Equal(estoqueAntes - 1, (await _client.GetFromJsonAsync<ProdutoDto>("/api/v1/produtos/3"))!.Estoque);
    }

    [Fact]
    public async Task Fechar_no_pix_aplica_o_desconto()
    {
        var id = await CarrinhoCom((4, 1)); // Notebook 4589,00 com 15% no Pix

        var pedido = (await (await Fechar(id, FormaPagamento.Pix)).Content.ReadFromJsonAsync<PedidoDto>(Json))!;

        Assert.Equal(15m, pedido.Pagamento.DescontoPercentual);
        Assert.Equal(3900.65m, pedido.Pagamento.Total);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    [InlineData(17)]
    public async Task Credito_fora_de_6_a_16_parcelas_retorna_422(int? parcelas)
    {
        var id = await CarrinhoCom((5, 1));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Fechar(id, FormaPagamento.Credito, parcelas)).StatusCode);
    }

    [Fact]
    public async Task Fechar_carrinho_vazio_retorna_422()
    {
        var id = await CarrinhoCom();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Fechar(id, FormaPagamento.Debito)).StatusCode);
    }
}
