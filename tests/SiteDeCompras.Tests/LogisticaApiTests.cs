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
    private static readonly JsonSerializerOptions Json = FluxoLogistica.Json;
    private readonly FluxoLogistica _fluxo = new(factory);

    private Task<(HttpClient Client, string Email)> ClienteLogado() => _fluxo.ClienteLogado();
    private Task<HttpClient> ClienteComPapel(PapelUsuario papel) => _fluxo.ClienteComPapel(papel);
    private Task<PedidoDto> Comprar(HttpClient client) => _fluxo.Comprar(client);
    private static Task<EnvioDto> EnvioDoPedido(HttpClient client, Guid pedidoId) => FluxoLogistica.EnvioDoPedido(client, pedidoId);

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
    public async Task Envio_passa_pelo_cd_e_pelo_transporte_e_o_pedido_acompanha()
    {
        var (cliente, _) = await ClienteLogado();
        var pedido = await Comprar(cliente);
        var envioId = (await EnvioDoPedido(cliente, pedido.Id)).Id;
        var operador = await ClienteComPapel(PapelUsuario.OperadorCD);
        async Task<StatusPedido> StatusDoPedido() => (await cliente.GetFromJsonAsync<PedidoDto>($"/api/v1/pedidos/{pedido.Id}", Json))!.Status;

        // Dentro do CD o atalho não vale: o envio anda pelas telas.
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await operador.PostAsync($"/api/v1/admin/logistica/envios/{envioId}/avancar", null)).StatusCode);

        await FluxoLogistica.Separar(operador, envioId);
        Assert.Equal(StatusPedido.EmPreparacao, await StatusDoPedido());
        var etiqueta = await FluxoLogistica.EmbalarEEtiquetar(operador, envioId);
        Assert.Equal(StatusPedido.EmPreparacao, await StatusDoPedido());
        await FluxoLogistica.Expedir(operador, etiqueta.CodigoRastreio);
        Assert.Equal(StatusPedido.Enviado, await StatusDoPedido());

        EnvioDto envio;
        do
        {
            envio = await FluxoLogistica.Ler<EnvioDto>(await operador.PostAsync($"/api/v1/admin/logistica/envios/{envioId}/avancar", null));
        } while (envio.Status != StatusEnvio.Entregue);

        Assert.Equal(StatusPedido.Entregue, await StatusDoPedido());
        Assert.Equal(Enum.GetValues<StatusEnvio>(), envio.Eventos.Select(e => e.Status));
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
        var codigo = (await FluxoLogistica.LevarAteProntoParaEnvio(admin, envioId)).CodigoRastreio;

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

    // O 422 do "avançar" é de quem tem permissão: o envio ainda está no CD.
    [Theory]
    [InlineData(PapelUsuario.Cliente, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden)]
    [InlineData(PapelUsuario.Atendimento, HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden)]
    [InlineData(PapelUsuario.OperadorCD, HttpStatusCode.OK, HttpStatusCode.UnprocessableEntity, HttpStatusCode.Created)]
    [InlineData(PapelUsuario.Entregador, HttpStatusCode.OK, HttpStatusCode.UnprocessableEntity, HttpStatusCode.Forbidden)]
    public async Task Papeis_da_logistica(PapelUsuario papel, HttpStatusCode consultar, HttpStatusCode avancar, HttpStatusCode gerarOnda)
    {
        var (cliente, _) = await ClienteLogado();
        var pedido = await Comprar(cliente);
        var envioId = (await EnvioDoPedido(cliente, pedido.Id)).Id;
        var client = await ClienteComPapel(papel);

        Assert.Equal(consultar, (await client.GetAsync("/api/v1/admin/logistica/envios")).StatusCode);
        Assert.Equal(consultar, (await client.GetAsync("/api/v1/admin/logistica/estoque")).StatusCode);
        Assert.Equal(avancar, (await client.PostAsync($"/api/v1/admin/logistica/envios/{envioId}/avancar", null)).StatusCode);
        Assert.Equal(gerarOnda, (await client.PostAsync("/api/v1/admin/logistica/ondas", null)).StatusCode);
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
