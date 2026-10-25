using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SiteDeCompras.Application.Carrinhos;
using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Enderecos;
using SiteDeCompras.Application.Logistica;
using SiteDeCompras.Application.Pedidos;
using SiteDeCompras.Application.Usuarios;
using SiteDeCompras.Domain.Logistica;
using SiteDeCompras.Domain.Pagamentos;
using SiteDeCompras.Domain.Pedidos;
using SiteDeCompras.Domain.Usuarios;
using SiteDeCompras.Infrastructure.Persistence;

namespace SiteDeCompras.Tests;

public class LogisticaApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private const string Senha = "Bolo-de-fuba-quentinho";

    private async Task<(HttpClient Client, string Email)> ClienteLogado()
    {
        var client = factory.CreateClient();
        var email = $"c_{Guid.NewGuid():N}@exemplo.com";
        (await client.PostAsJsonAsync("/api/v1/auth/cadastro", new CadastroRequest { Nome = "Comprador", Email = email, Senha = Senha }))
            .EnsureSuccessStatusCode();
        return (client, email);
    }

    /// <summary>Cria uma conta, troca o papel direto no banco e entra de novo (trocar o papel derruba a sessão).</summary>
    private async Task<HttpClient> ClienteComPapel(PapelUsuario papel)
    {
        var (_, email) = await ClienteLogado();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var usuario = await db.Usuarios.SingleAsync(u => u.EmailNormalizado == email);
            usuario.DefinirPapel(papel);
            await db.SaveChangesAsync();
        }
        var client = factory.CreateClient();
        (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = email, Senha = Senha })).EnsureSuccessStatusCode();
        return client;
    }

    // O seed tem 10 unidades de cada produto; alternar entre os 6 evita esgotar um só.
    private static int _compras;

    private static async Task<PedidoDto> Comprar(HttpClient client)
    {
        var produtoId = Interlocked.Increment(ref _compras) % 6 + 1;
        var carrinho = (await (await client.PostAsync("/api/v1/carrinhos", null)).Content.ReadFromJsonAsync<CarrinhoDto>())!;
        (await client.PostAsJsonAsync($"/api/v1/carrinhos/{carrinho.Id}/itens", new AdicionarItemRequest { ProdutoId = produtoId })).EnsureSuccessStatusCode();
        var resposta = await client.PostAsJsonAsync("/api/v1/pedidos", new FecharPedidoRequest
        {
            CarrinhoId = carrinho.Id, FormaPagamento = FormaPagamento.Pix, EnderecoEntrega = Exemplos.EnderecoRequest(),
        }, Json);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<PedidoDto>(Json))!;
    }

    private static async Task<EnvioDto> EnvioDoPedido(HttpClient client, Guid pedidoId) =>
        Assert.Single((await client.GetFromJsonAsync<List<EnvioDto>>($"/api/v1/pedidos/{pedidoId}/rastreio", Json))!);

    [Fact]
    public async Task Pedido_pago_abre_um_envio_na_fila_com_o_endereco()
    {
        var (cliente, _) = await ClienteLogado();

        var pedido = await Comprar(cliente);

        Assert.Equal("Belo Horizonte", pedido.EnderecoEntrega?.Cidade);
        Assert.Equal("30130010", pedido.EnderecoEntrega?.Cep);
        var envio = await EnvioDoPedido(cliente, pedido.Id);
        Assert.Equal(StatusEnvio.AguardandoSeparacao, envio.Status);
        Assert.Null(envio.CodigoRastreio);
        Assert.True(envio.PrevisaoEntrega > envio.PrazoDespacho);
        Assert.Single(envio.Eventos);
    }

    [Fact]
    public async Task Pedido_sem_endereco_retorna_400()
    {
        var client = factory.CreateClient();
        var carrinho = (await (await client.PostAsync("/api/v1/carrinhos", null)).Content.ReadFromJsonAsync<CarrinhoDto>())!;
        await client.PostAsJsonAsync($"/api/v1/carrinhos/{carrinho.Id}/itens", new AdicionarItemRequest { ProdutoId = 5 });

        var resposta = await client.PostAsJsonAsync("/api/v1/pedidos",
            new FecharPedidoRequest { CarrinhoId = carrinho.Id, FormaPagamento = FormaPagamento.Pix }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Operador_leva_o_envio_ate_a_entrega_e_o_pedido_acompanha()
    {
        var (cliente, _) = await ClienteLogado();
        var pedido = await Comprar(cliente);
        var envioId = (await EnvioDoPedido(cliente, pedido.Id)).Id;
        var operador = await ClienteComPapel(PapelUsuario.OperadorCD);

        var esperado = new Dictionary<StatusEnvio, StatusPedido>
        {
            [StatusEnvio.EmSeparacao] = StatusPedido.EmPreparacao,
            [StatusEnvio.ProntoParaEnvio] = StatusPedido.EmPreparacao,
            [StatusEnvio.EmTransito] = StatusPedido.Enviado,
            [StatusEnvio.Entregue] = StatusPedido.Entregue,
        };
        EnvioDto envio;
        do
        {
            var resposta = await operador.PostAsync($"/api/v1/admin/logistica/envios/{envioId}/avancar", null);
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            envio = (await resposta.Content.ReadFromJsonAsync<EnvioDto>(Json))!;
            if (esperado.TryGetValue(envio.Status, out var statusPedido))
                Assert.Equal(statusPedido, (await cliente.GetFromJsonAsync<PedidoDto>($"/api/v1/pedidos/{pedido.Id}", Json))!.Status);
        } while (envio.Status != StatusEnvio.Entregue);

        Assert.Equal(9, envio.Eventos.Count);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await operador.PostAsync($"/api/v1/admin/logistica/envios/{envioId}/avancar", null)).StatusCode);
    }

    [Fact]
    public async Task Rastreio_publico_mostra_so_cidade_previsao_e_eventos()
    {
        var (cliente, _) = await ClienteLogado();
        var pedido = await Comprar(cliente);
        var envioId = (await EnvioDoPedido(cliente, pedido.Id)).Id;
        var admin = await factory.ClienteAdminAsync();
        for (var i = 0; i < 4; i++) // até ProntoParaEnvio, quando nasce o código
            (await admin.PostAsync($"/api/v1/admin/logistica/envios/{envioId}/avancar", null)).EnsureSuccessStatusCode();
        var codigo = (await EnvioDoPedido(cliente, pedido.Id)).CodigoRastreio!;

        var resposta = await factory.CreateClient().GetAsync($"/api/v1/rastreio/{codigo.ToLowerInvariant()}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.Contains("Belo Horizonte", corpo);
        Assert.DoesNotContain("Maria", corpo);
        Assert.DoesNotContain("Afonso Pena", corpo);
        Assert.DoesNotContain(pedido.Id.ToString(), corpo);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateClient().GetAsync("/api/v1/rastreio/SC000000000BR")).StatusCode);
    }

    [Fact]
    public async Task Rastreio_do_pedido_segue_a_regra_do_dono()
    {
        var (dono, _) = await ClienteLogado();
        var pedido = await Comprar(dono);
        var (outro, _) = await ClienteLogado();

        Assert.Equal(HttpStatusCode.NotFound, (await outro.GetAsync($"/api/v1/pedidos/{pedido.Id}/rastreio")).StatusCode);
    }

    [Theory]
    [InlineData(PapelUsuario.Cliente, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden)]
    [InlineData(PapelUsuario.Atendimento, HttpStatusCode.OK, HttpStatusCode.Forbidden)]
    [InlineData(PapelUsuario.OperadorCD, HttpStatusCode.OK, HttpStatusCode.OK)]
    [InlineData(PapelUsuario.Entregador, HttpStatusCode.OK, HttpStatusCode.OK)]
    public async Task Papeis_da_logistica(PapelUsuario papel, HttpStatusCode consultar, HttpStatusCode avancar)
    {
        var (cliente, _) = await ClienteLogado();
        var pedido = await Comprar(cliente);
        var envioId = (await EnvioDoPedido(cliente, pedido.Id)).Id;
        var client = await ClienteComPapel(papel);

        Assert.Equal(consultar, (await client.GetAsync("/api/v1/admin/logistica/envios")).StatusCode);
        Assert.Equal(avancar, (await client.PostAsync($"/api/v1/admin/logistica/envios/{envioId}/avancar", null)).StatusCode);
    }

    [Fact]
    public async Task Fila_sem_login_retorna_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/v1/admin/logistica/envios")).StatusCode);
    }

    [Fact]
    public async Task Fila_filtra_por_status_e_busca_pelo_numero_do_pedido()
    {
        var (cliente, _) = await ClienteLogado();
        var pedido = await Comprar(cliente);
        var admin = await factory.ClienteAdminAsync();
        var numero = pedido.Id.ToString()[..8].ToUpperInvariant();

        var fila = await admin.GetFromJsonAsync<PagedResult<EnvioResumoDto>>(
            $"/api/v1/admin/logistica/envios?status=AguardandoSeparacao&busca={numero}", Json);

        var linha = Assert.Single(fila!.Itens);
        Assert.Equal(pedido.Id, linha.PedidoId);
        Assert.Equal("MG", linha.Uf);
    }

    [Fact]
    public async Task Painel_conta_os_envios_por_etapa()
    {
        var (cliente, _) = await ClienteLogado();
        await Comprar(cliente);
        var admin = await factory.ClienteAdminAsync();

        var painel = await admin.GetFromJsonAsync<PainelLogisticaDto>("/api/v1/admin/logistica/painel", Json);

        Assert.True(painel!.PorStatus[StatusEnvio.AguardandoSeparacao] >= 1);
        Assert.Equal(Enum.GetValues<StatusEnvio>().Length, painel.PorStatus.Count);
    }

    [Fact]
    public async Task Meus_pedidos_lista_so_os_da_conta()
    {
        var (cliente, _) = await ClienteLogado();
        var pedido = await Comprar(cliente);
        await Comprar((await ClienteLogado()).Client);

        var lista = await cliente.GetFromJsonAsync<List<PedidoResumoDto>>("/api/v1/conta/pedidos", Json);

        var linha = Assert.Single(lista!);
        Assert.Equal(pedido.Id, linha.Id);
        Assert.Equal(pedido.Itens[0].NomeProduto, linha.PrimeiroItem);
    }

    [Fact]
    public async Task Enderecos_salvos_sao_da_conta()
    {
        var (dono, _) = await ClienteLogado();
        var (outro, _) = await ClienteLogado();

        var criado = await dono.PostAsJsonAsync("/api/v1/conta/enderecos", Exemplos.EnderecoRequest("01310-100"));
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var endereco = (await criado.Content.ReadFromJsonAsync<EnderecoSalvoDto>(Json))!;
        Assert.Equal("01310100", endereco.Endereco.Cep);

        Assert.Empty((await outro.GetFromJsonAsync<List<EnderecoSalvoDto>>("/api/v1/conta/enderecos", Json))!);
        Assert.Equal(HttpStatusCode.NotFound, (await outro.DeleteAsync($"/api/v1/conta/enderecos/{endereco.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await dono.DeleteAsync($"/api/v1/conta/enderecos/{endereco.Id}")).StatusCode);
        Assert.Empty((await dono.GetFromJsonAsync<List<EnderecoSalvoDto>>("/api/v1/conta/enderecos", Json))!);
    }

    [Fact]
    public async Task Endereco_com_uf_invalida_retorna_422()
    {
        var (dono, _) = await ClienteLogado();

        var resposta = await dono.PostAsJsonAsync("/api/v1/conta/enderecos", Exemplos.EnderecoRequest() with { Uf = "XX" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }
}
