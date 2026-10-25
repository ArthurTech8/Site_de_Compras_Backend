using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SiteDeCompras.Application.Carrinhos;
using SiteDeCompras.Application.Logistica;
using SiteDeCompras.Application.Pedidos;
using SiteDeCompras.Application.Usuarios;
using SiteDeCompras.Domain.Logistica;
using SiteDeCompras.Domain.Pagamentos;
using SiteDeCompras.Domain.Usuarios;
using SiteDeCompras.Infrastructure.Persistence;

namespace SiteDeCompras.Tests;

/// <summary>Passos comuns dos testes de logística: comprar, entrar com um papel e levar um envio pelo CD.</summary>
public class FluxoLogistica(ApiFactory factory)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private const string Senha = "Bolo-de-fuba-quentinho";
    private const string Rota = "/api/v1/admin/logistica";

    // O seed tem 10 unidades de cada produto; alternar entre os pequenos evita esgotar um só.
    private static readonly int[] Produtos = [3, 4, 5, 6];
    private static int _compras;

    public async Task<(HttpClient Client, string Email)> ClienteLogado()
    {
        var client = factory.CreateClient();
        var email = $"c_{Guid.NewGuid():N}@exemplo.com";
        (await client.PostAsJsonAsync("/api/v1/auth/cadastro", new CadastroRequest { Nome = "Comprador", Email = email, Senha = Senha }))
            .EnsureSuccessStatusCode();
        return (client, email);
    }

    /// <summary>Cria uma conta, troca o papel direto no banco e entra de novo (trocar o papel derruba a sessão).</summary>
    public async Task<HttpClient> ClienteComPapel(PapelUsuario papel)
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

    public async Task<PedidoDto> Comprar(HttpClient client, int? produtoId = null, int quantidade = 1)
    {
        var produto = produtoId ?? Produtos[Interlocked.Increment(ref _compras) % Produtos.Length];
        var carrinho = (await (await client.PostAsync("/api/v1/carrinhos", null)).Content.ReadFromJsonAsync<CarrinhoDto>())!;
        (await client.PostAsJsonAsync($"/api/v1/carrinhos/{carrinho.Id}/itens", new AdicionarItemRequest { ProdutoId = produto, Quantidade = quantidade }))
            .EnsureSuccessStatusCode();
        var resposta = await client.PostAsJsonAsync("/api/v1/pedidos", new FecharPedidoRequest
        {
            CarrinhoId = carrinho.Id, FormaPagamento = FormaPagamento.Pix, EnderecoEntrega = Exemplos.EnderecoRequest(),
        }, Json);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<PedidoDto>(Json))!;
    }

    public static async Task<EnvioDto> EnvioDoPedido(HttpClient client, Guid pedidoId) =>
        Assert.Single((await client.GetFromJsonAsync<List<EnvioDto>>($"/api/v1/pedidos/{pedidoId}/rastreio", Json))!);

    public static async Task<T> Ler<T>(HttpResponseMessage resposta, HttpStatusCode esperado = HttpStatusCode.OK)
    {
        Assert.True(resposta.StatusCode == esperado, $"Esperava {esperado}, veio {resposta.StatusCode}: {await resposta.Content.ReadAsStringAsync()}");
        return (await resposta.Content.ReadFromJsonAsync<T>(Json))!;
    }

    /// <summary>Gera ondas e bipa tudo o que elas pedem até o envio ficar separado.</summary>
    public static async Task Separar(HttpClient operador, Guid envioId)
    {
        for (var tentativa = 0; tentativa < 5; tentativa++)
        {
            var envio = await operador.GetFromJsonAsync<EnvioDto>($"{Rota}/envios/{envioId}", Json);
            if (envio!.Status != StatusEnvio.AguardandoSeparacao && envio.Status != StatusEnvio.EmSeparacao)
                return;

            var ondas = await operador.GetFromJsonAsync<List<OndaResumoDto>>($"{Rota}/ondas", Json);
            foreach (var aberta in ondas!.Where(o => o.Status == StatusOnda.Aberta))
                await SepararOnda(operador, aberta.Id);
            if (envio.Status == StatusEnvio.AguardandoSeparacao)
            {
                var onda = await Ler<OndaDto>(await operador.PostAsJsonAsync($"{Rota}/ondas", new GerarOndaRequest { Envios = 30 }, Json), HttpStatusCode.Created);
                await SepararOnda(operador, onda.Id);
            }
        }
        Assert.Fail("O envio não foi separado.");
    }

    public static async Task SepararOnda(HttpClient operador, int ondaId)
    {
        var onda = await operador.GetFromJsonAsync<OndaDto>($"{Rota}/ondas/{ondaId}", Json);
        foreach (var item in onda!.Coleta)
            for (var i = item.Separadas; i < item.Quantidade; i++)
                await Ler<ResultadoOndaDto>(await operador.PostAsJsonAsync($"{Rota}/ondas/{ondaId}/bipar", new BipagemRequest { Codigo = item.Sku }, Json));
        await Ler<OndaDto>(await operador.PostAsync($"{Rota}/ondas/{ondaId}/concluir", null));
    }

    /// <summary>Confere os itens, fecha a caixa sugerida com o peso esperado e gera a etiqueta.</summary>
    public static async Task<EtiquetaDto> EmbalarEEtiquetar(HttpClient operador, Guid envioId)
    {
        var bancada = await operador.GetFromJsonAsync<EmbalagemDto>($"{Rota}/envios/{envioId}/embalagem", Json);
        foreach (var item in bancada!.Itens)
            for (var i = item.Conferidas; i < item.Quantidade; i++)
                await Ler<EmbalagemDto>(await operador.PostAsJsonAsync($"{Rota}/envios/{envioId}/conferir", new BipagemRequest { Codigo = item.Sku }, Json));

        var caixa = bancada.Caixas.Single(c => c.Codigo == bancada.CaixaSugerida);
        await Ler<EmbalagemDto>(await operador.PostAsJsonAsync($"{Rota}/envios/{envioId}/embalar",
            new EmbalarRequest { Caixa = caixa.Codigo, PesoGramas = caixa.PesoEsperadoGramas }, Json));
        return await Ler<EtiquetaDto>(await operador.PostAsync($"{Rota}/envios/{envioId}/etiqueta", null));
    }

    public static async Task<EtiquetaDto> LevarAteProntoParaEnvio(HttpClient operador, Guid envioId)
    {
        await Separar(operador, envioId);
        return await EmbalarEEtiquetar(operador, envioId);
    }

    /// <summary>Abre (ou reaproveita) o manifesto, bipa a etiqueta, fecha e confirma a coleta.</summary>
    public static async Task<ManifestoDto> Expedir(HttpClient operador, string codigoRastreio)
    {
        var doca = await operador.GetFromJsonAsync<ExpedicaoDto>($"{Rota}/manifestos", Json);
        var aberto = doca!.Manifestos.FirstOrDefault(m => m.Status == StatusManifesto.Aberto);
        var id = aberto?.Id ?? (await Ler<ManifestoDto>(await operador.PostAsync($"{Rota}/manifestos", null), HttpStatusCode.Created)).Id;

        await Ler<ManifestoDto>(await operador.PostAsJsonAsync($"{Rota}/manifestos/{id}/pacotes", new BipagemRequest { Codigo = codigoRastreio }, Json));
        await Ler<ManifestoDto>(await operador.PostAsync($"{Rota}/manifestos/{id}/fechar", null));
        return await Ler<ManifestoDto>(await operador.PostAsync($"{Rota}/manifestos/{id}/coletar", null));
    }
}
