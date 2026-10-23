using System.Net;
using System.Net.Http.Json;
using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Produtos;

namespace SiteDeCompras.Tests;

public class ProdutosApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Lista_os_produtos_iniciais()
    {
        var pagina = await _client.GetFromJsonAsync<PagedResult<ProdutoDto>>("/api/v1/produtos?tamanhoPagina=100");

        Assert.NotNull(pagina);
        Assert.Contains(pagina.Itens, p => p.Nome == "Geladeira" && p.Preco == 2300.00m);
    }

    [Fact]
    public async Task Filtra_e_ordena_por_preco()
    {
        var pagina = await _client.GetFromJsonAsync<PagedResult<ProdutoDto>>(
            "/api/v1/produtos?precoMax=2300&ordenacao=MenorPreco");

        Assert.NotNull(pagina);
        Assert.Equal(["Caneca", "Celular Xiaomi", "Playstation 4", "Geladeira"], pagina.Itens.Select(p => p.Nome));
    }

    [Fact]
    public async Task Busca_por_nome()
    {
        var pagina = await _client.GetFromJsonAsync<PagedResult<ProdutoDto>>("/api/v1/produtos?busca=note");

        Assert.NotNull(pagina);
        Assert.Equal("Notebook", Assert.Single(pagina.Itens).Nome);
    }

    [Fact]
    public async Task Produto_inexistente_retorna_404()
    {
        var resposta = await _client.GetAsync("/api/v1/produtos/9999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Cria_atualiza_e_remove_produto()
    {
        var admin = await factory.ClienteAdminAsync();
        var criar = await admin.PostAsJsonAsync("/api/v1/produtos",
            new SalvarProdutoRequest { Nome = "Fone Bluetooth", Preco = 199.90m, Estoque = 3 });
        Assert.Equal(HttpStatusCode.Created, criar.StatusCode);
        var produto = await criar.Content.ReadFromJsonAsync<ProdutoDto>();
        Assert.NotNull(produto);
        Assert.Equal($"/api/v1/produtos/{produto.Id}", criar.Headers.Location?.AbsolutePath);

        var atualizar = await admin.PutAsJsonAsync($"/api/v1/produtos/{produto.Id}",
            new SalvarProdutoRequest { Nome = "Fone Bluetooth", Preco = 149.90m, Estoque = 3 });
        Assert.Equal(149.90m, (await atualizar.Content.ReadFromJsonAsync<ProdutoDto>())!.Preco);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/produtos/{produto.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/v1/produtos/{produto.Id}")).StatusCode);
    }

    [Fact]
    public async Task Produto_invalido_retorna_400()
    {
        var admin = await factory.ClienteAdminAsync();
        var resposta = await admin.PostAsJsonAsync("/api/v1/produtos", new SalvarProdutoRequest { Nome = "", Preco = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//site-malicioso.com/x.png")]
    [InlineData("data:image/svg+xml,<svg onload=alert(1)>")]
    public async Task Imagem_com_protocolo_perigoso_retorna_422(string url)
    {
        var admin = await factory.ClienteAdminAsync();
        var resposta = await admin.PostAsJsonAsync("/api/v1/produtos",
            new SalvarProdutoRequest { Nome = "Teste", Preco = 10, Estoque = 1, ImagemUrl = url });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Health_check_responde()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health")).StatusCode);
    }
}
