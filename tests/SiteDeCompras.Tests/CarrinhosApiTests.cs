using System.Net;
using System.Net.Http.Json;
using SiteDeCompras.Application.Carrinhos;

namespace SiteDeCompras.Tests;

public class CarrinhosApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<CarrinhoDto> CriarCarrinho()
    {
        var resposta = await _client.PostAsync("/api/v1/carrinhos", null);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<CarrinhoDto>())!;
    }

    private async Task<HttpResponseMessage> Adicionar(Guid carrinhoId, int produtoId, int quantidade) =>
        await _client.PostAsJsonAsync($"/api/v1/carrinhos/{carrinhoId}/itens",
            new AdicionarItemRequest { ProdutoId = produtoId, Quantidade = quantidade });

    [Fact]
    public async Task Fluxo_completo_do_carrinho()
    {
        var carrinho = await CriarCarrinho();
        Assert.Empty(carrinho.Itens);

        await Adicionar(carrinho.Id, 5, 2);   // Caneca 59,00
        await Adicionar(carrinho.Id, 1, 1);   // Geladeira 2300,00
        var atual = await _client.GetFromJsonAsync<CarrinhoDto>($"/api/v1/carrinhos/{carrinho.Id}");
        Assert.Equal(2, atual!.Itens.Count);
        Assert.Equal(2418.00m, atual.Total);

        var alterar = await _client.PutAsJsonAsync($"/api/v1/carrinhos/{carrinho.Id}/itens/5",
            new AlterarQuantidadeRequest { Quantidade = 1 });
        Assert.Equal(2359.00m, (await alterar.Content.ReadFromJsonAsync<CarrinhoDto>())!.Total);

        var remover = await _client.DeleteAsync($"/api/v1/carrinhos/{carrinho.Id}/itens/1");
        var aposRemover = await remover.Content.ReadFromJsonAsync<CarrinhoDto>();
        Assert.Equal(5, Assert.Single(aposRemover!.Itens).ProdutoId);

        var limpar = await _client.DeleteAsync($"/api/v1/carrinhos/{carrinho.Id}/itens");
        Assert.Empty((await limpar.Content.ReadFromJsonAsync<CarrinhoDto>())!.Itens);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/v1/carrinhos/{carrinho.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/v1/carrinhos/{carrinho.Id}")).StatusCode);
    }

    [Fact]
    public async Task Estoque_insuficiente_retorna_422()
    {
        var carrinho = await CriarCarrinho();

        var resposta = await Adicionar(carrinho.Id, 4, 11);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Produto_inexistente_retorna_404()
    {
        var carrinho = await CriarCarrinho();

        Assert.Equal(HttpStatusCode.NotFound, (await Adicionar(carrinho.Id, 9999, 1)).StatusCode);
    }

    [Fact]
    public async Task Remover_item_que_nao_esta_no_carrinho_retorna_404()
    {
        var carrinho = await CriarCarrinho();

        var resposta = await _client.DeleteAsync($"/api/v1/carrinhos/{carrinho.Id}/itens/3");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
