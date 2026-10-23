using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SiteDeCompras.Application.Carrinhos;
using SiteDeCompras.Application.Pedidos;
using SiteDeCompras.Application.Usuarios;
using SiteDeCompras.Domain.Pagamentos;

namespace SiteDeCompras.Tests;

/// <summary>Pedido com dono não pode ser lido por outra pessoa só porque ela tem o id (IDOR).</summary>
public class PedidosDonoTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private async Task<HttpClient> ClienteLogado()
    {
        var client = factory.CreateClient();
        (await client.PostAsJsonAsync("/api/v1/auth/cadastro", new CadastroRequest
        {
            Nome = "Comprador", Email = $"c_{Guid.NewGuid():N}@exemplo.com", Senha = "Bolo-de-fuba-quentinho",
        })).EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<Guid> Comprar(HttpClient client)
    {
        var carrinho = (await (await client.PostAsync("/api/v1/carrinhos", null)).Content.ReadFromJsonAsync<CarrinhoDto>())!;
        (await client.PostAsJsonAsync($"/api/v1/carrinhos/{carrinho.Id}/itens", new AdicionarItemRequest { ProdutoId = 5 })).EnsureSuccessStatusCode();
        var resposta = await client.PostAsJsonAsync("/api/v1/pedidos",
            new FecharPedidoRequest { CarrinhoId = carrinho.Id, FormaPagamento = FormaPagamento.Pix }, Json);
        return (await resposta.Content.ReadFromJsonAsync<PedidoDto>(Json))!.Id;
    }

    [Fact]
    public async Task Pedido_com_dono_so_aparece_para_o_dono_e_o_admin()
    {
        var dono = await ClienteLogado();
        var pedidoId = await Comprar(dono);

        Assert.Equal(HttpStatusCode.OK, (await dono.GetAsync($"/api/v1/pedidos/{pedidoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await (await ClienteLogado()).GetAsync($"/api/v1/pedidos/{pedidoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateClient().GetAsync($"/api/v1/pedidos/{pedidoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await (await factory.ClienteAdminAsync()).GetAsync($"/api/v1/pedidos/{pedidoId}")).StatusCode);
    }

    [Fact]
    public async Task Pedido_sem_login_continua_acessivel_pelo_id()
    {
        var anonimo = factory.CreateClient();
        var pedidoId = await Comprar(anonimo);

        Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient().GetAsync($"/api/v1/pedidos/{pedidoId}")).StatusCode);
    }
}
