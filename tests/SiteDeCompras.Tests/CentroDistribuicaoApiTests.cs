using System.Net;
using System.Net.Http.Json;
using SiteDeCompras.Application.Logistica;
using SiteDeCompras.Application.Produtos;
using SiteDeCompras.Domain.Logistica;
using SiteDeCompras.Domain.Usuarios;

namespace SiteDeCompras.Tests;

public class CentroDistribuicaoApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Rota = "/api/v1/admin/logistica";
    private readonly FluxoLogistica _fluxo = new(factory);
    private static readonly System.Text.Json.JsonSerializerOptions Json = FluxoLogistica.Json;

    private async Task<(HttpClient Cliente, Guid PedidoId, Guid EnvioId)> CompraNova(int? produtoId = null, int quantidade = 1)
    {
        var (cliente, _) = await _fluxo.ClienteLogado();
        var pedido = await _fluxo.Comprar(cliente, produtoId, quantidade);
        return (cliente, pedido.Id, (await FluxoLogistica.EnvioDoPedido(cliente, pedido.Id)).Id);
    }

    /// <summary>Separa tudo o que está pendente, para o próximo teste começar com a fila vazia.</summary>
    private static async Task EsvaziarFila(HttpClient admin)
    {
        var ondas = await admin.GetFromJsonAsync<List<OndaResumoDto>>($"{Rota}/ondas", Json);
        foreach (var aberta in ondas!.Where(o => o.Status == StatusOnda.Aberta))
            await FluxoLogistica.SepararOnda(admin, aberta.Id);
        while ((await admin.PostAsJsonAsync($"{Rota}/ondas", new GerarOndaRequest { Envios = 30 }, Json)) is { StatusCode: HttpStatusCode.Created } r)
            await FluxoLogistica.SepararOnda(admin, (await r.Content.ReadFromJsonAsync<OndaDto>(Json))!.Id);
    }

    [Fact]
    public async Task Onda_monta_a_rota_pela_prateleira_e_cada_bipe_vai_para_o_envio_mais_urgente()
    {
        var admin = await factory.ClienteAdminAsync();
        await EsvaziarFila(admin);
        var (_, _, ps4) = await CompraNova(produtoId: 6); // B-11-1
        var (_, _, canecas) = await CompraNova(produtoId: 5, quantidade: 2); // A-03-2

        var onda = await FluxoLogistica.Ler<OndaDto>(await admin.PostAsync($"{Rota}/ondas", null), HttpStatusCode.Created);

        Assert.Equal(["A-03-2", "B-11-1"], onda.Coleta.Select(c => c.Local));
        Assert.Equal(2, onda.Coleta[0].Quantidade);
        Assert.Equal(2, onda.Envios.Count);

        var bipe = await FluxoLogistica.Ler<ResultadoOndaDto>(await admin.PostAsJsonAsync($"{Rota}/ondas/{onda.Id}/bipar", new BipagemRequest { Codigo = "can-0005" }, Json));
        Assert.Equal("Caneca: 1 de 2.", bipe.Mensagem);
        await admin.PostAsJsonAsync($"{Rota}/ondas/{onda.Id}/bipar", new BipagemRequest { Codigo = "CAN-0005" }, Json);

        var demais = await admin.PostAsJsonAsync($"{Rota}/ondas/{onda.Id}/bipar", new BipagemRequest { Codigo = "CAN-0005" }, Json);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, demais.StatusCode);
        var desconhecido = await admin.PostAsJsonAsync($"{Rota}/ondas/{onda.Id}/bipar", new BipagemRequest { Codigo = "GEL-0001" }, Json);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, desconhecido.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsync($"{Rota}/ondas/{onda.Id}/concluir", null)).StatusCode);

        var ultimo = await FluxoLogistica.Ler<ResultadoOndaDto>(await admin.PostAsJsonAsync($"{Rota}/ondas/{onda.Id}/bipar", new BipagemRequest { Codigo = "PS4-0006" }, Json));
        Assert.StartsWith("Envio do pedido #", ultimo.Mensagem);
        Assert.All(ultimo.Onda.Envios, e => Assert.Equal(StatusEnvio.Separado, e.Status));
        await FluxoLogistica.Ler<OndaDto>(await admin.PostAsync($"{Rota}/ondas/{onda.Id}/concluir", null));
        Assert.Equal(StatusEnvio.Separado, (await admin.GetFromJsonAsync<EnvioDto>($"{Rota}/envios/{canecas}", Json))!.Status);
        Assert.Equal(StatusEnvio.Separado, (await admin.GetFromJsonAsync<EnvioDto>($"{Rota}/envios/{ps4}", Json))!.Status);
    }

    [Fact]
    public async Task Falta_na_prateleira_tira_o_envio_das_ondas_ate_o_inventario()
    {
        var admin = await factory.ClienteAdminAsync();
        await EsvaziarFila(admin);
        var (cliente, pedidoId, envioId) = await CompraNova(produtoId: 4); // Notebook
        var onda = await FluxoLogistica.Ler<OndaDto>(await admin.PostAsync($"{Rota}/ondas", null), HttpStatusCode.Created);

        var falta = await FluxoLogistica.Ler<ResultadoOndaDto>(await admin.PostAsJsonAsync($"{Rota}/ondas/{onda.Id}/falta", new FaltaRequest { ProdutoId = 4 }, Json));

        Assert.Empty(falta.Onda.Envios);
        var envio = await admin.GetFromJsonAsync<EnvioDto>($"{Rota}/envios/{envioId}", Json);
        Assert.Equal(StatusEnvio.AguardandoSeparacao, envio!.Status);
        Assert.Contains(envio.Eventos, e => !e.Publico && e.Descricao.Contains("Notebook"));
        Assert.DoesNotContain(await FluxoLogistica.EnvioDoPedido(cliente, pedidoId) is var doCliente ? doCliente.Eventos : [], e => !e.Publico);

        var estoque = await admin.GetFromJsonAsync<List<PosicaoEstoqueDto>>($"{Rota}/estoque", Json);
        var notebook = estoque!.Single(p => p.ProdutoId == 4);
        Assert.Equal(1, notebook.Divergencia);
        Assert.Equal(1, notebook.Reservado);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsync($"{Rota}/ondas", null)).StatusCode);
        Assert.Equal(1, (await admin.GetFromJsonAsync<PainelLogisticaDto>($"{Rota}/painel", Json))!.DivergenciasAbertas);

        // Achou 6 na prateleira: 1 é do pedido, sobram 5 para vender, e o envio volta às ondas.
        var inventario = await FluxoLogistica.Ler<InventarioDto>(await admin.PostAsJsonAsync($"{Rota}/estoque/4/inventario", new InventarioRequest { QuantidadeContada = 6 }, Json));
        Assert.Equal(5, inventario.Posicao.Disponivel);
        Assert.Equal(0, inventario.Posicao.Divergencia);

        var nova = await FluxoLogistica.Ler<OndaDto>(await admin.PostAsync($"{Rota}/ondas", null), HttpStatusCode.Created);
        Assert.Contains(nova.Envios, e => e.Id == envioId);
        await FluxoLogistica.SepararOnda(admin, nova.Id);
    }

    [Fact]
    public async Task Inventario_que_nao_cobre_os_pedidos_mantem_a_divergencia()
    {
        var admin = await factory.ClienteAdminAsync();
        await EsvaziarFila(admin);
        var (_, _, _) = await CompraNova(produtoId: 3, quantidade: 2); // Celular
        var onda = await FluxoLogistica.Ler<OndaDto>(await admin.PostAsync($"{Rota}/ondas", null), HttpStatusCode.Created);
        await admin.PostAsJsonAsync($"{Rota}/ondas/{onda.Id}/falta", new FaltaRequest { ProdutoId = 3 }, Json);

        var inventario = await FluxoLogistica.Ler<InventarioDto>(await admin.PostAsJsonAsync($"{Rota}/estoque/3/inventario", new InventarioRequest { QuantidadeContada = 1 }, Json));

        Assert.Equal(0, inventario.Posicao.Disponivel);
        Assert.Equal(2, inventario.Posicao.Divergencia);
        Assert.Contains("Faltam 1", inventario.Mensagem);

        await FluxoLogistica.Ler<InventarioDto>(await admin.PostAsJsonAsync($"{Rota}/estoque/3/inventario", new InventarioRequest { QuantidadeContada = 4 }, Json));
        await EsvaziarFila(admin);
    }

    [Fact]
    public async Task Embalagem_confere_os_itens_a_caixa_e_o_peso()
    {
        var admin = await factory.ClienteAdminAsync();
        var (_, _, envioId) = await CompraNova(produtoId: 5, quantidade: 2); // 2 canecas
        await FluxoLogistica.Separar(admin, envioId);

        var bancada = await admin.GetFromJsonAsync<EmbalagemDto>($"{Rota}/envios/{envioId}/embalagem", Json);
        Assert.Equal("M", bancada!.CaixaSugerida);
        Assert.False(bancada.Caixas.Single(c => c.Codigo == "P").Cabe);
        var m = bancada.Caixas.Single(c => c.Codigo == "M");
        Assert.Equal(2 * 450 + 250, m.PesoEsperadoGramas);

        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await admin.PostAsJsonAsync($"{Rota}/envios/{envioId}/conferir", new BipagemRequest { Codigo = "PS4-0006" }, Json)).StatusCode);
        await FluxoLogistica.Ler<EmbalagemDto>(await admin.PostAsJsonAsync($"{Rota}/envios/{envioId}/conferir", new BipagemRequest { Codigo = "CAN-0005" }, Json));
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await admin.PostAsJsonAsync($"{Rota}/envios/{envioId}/embalar", new EmbalarRequest { Caixa = "M", PesoGramas = 1150 }, Json)).StatusCode);
        await FluxoLogistica.Ler<EmbalagemDto>(await admin.PostAsJsonAsync($"{Rota}/envios/{envioId}/conferir", new BipagemRequest { Codigo = "CAN-0005" }, Json));

        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await admin.PostAsJsonAsync($"{Rota}/envios/{envioId}/embalar", new EmbalarRequest { Caixa = "P", PesoGramas = 1050 }, Json)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await admin.PostAsJsonAsync($"{Rota}/envios/{envioId}/embalar", new EmbalarRequest { Caixa = "M", PesoGramas = 600 }, Json)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync($"{Rota}/envios/{envioId}/etiqueta")).StatusCode);

        var embalado = await FluxoLogistica.Ler<EmbalagemDto>(await admin.PostAsJsonAsync($"{Rota}/envios/{envioId}/embalar",
            new EmbalarRequest { Caixa = "m", PesoGramas = 1180 }, Json));
        Assert.Equal(StatusEnvio.Embalado, embalado.Status);
        Assert.Equal(1180, embalado.Pacote!.PesoGramas);

        var etiqueta = await FluxoLogistica.Ler<EtiquetaDto>(await admin.PostAsync($"{Rota}/envios/{envioId}/etiqueta", null));
        Assert.Matches(@"^SC\d{9}BR$", etiqueta.CodigoRastreio);
        Assert.Equal("Maria Souza", etiqueta.Destino.Destinatario);
        Assert.Equal(2, etiqueta.Unidades);
        Assert.Equal(etiqueta.CodigoRastreio, (await admin.GetFromJsonAsync<EtiquetaDto>($"{Rota}/envios/{envioId}/etiqueta", Json))!.CodigoRastreio);
    }

    [Fact]
    public async Task Geladeira_sai_como_volume_avulso()
    {
        var admin = await factory.ClienteAdminAsync();
        var (_, _, envioId) = await CompraNova(produtoId: 1);
        await FluxoLogistica.Separar(admin, envioId);

        var bancada = await admin.GetFromJsonAsync<EmbalagemDto>($"{Rota}/envios/{envioId}/embalagem", Json);

        Assert.Equal("AVULSO", bancada!.CaixaSugerida);
        Assert.All(bancada.Caixas.Where(c => c.Codigo != "AVULSO"), c => Assert.False(c.Cabe));
        await FluxoLogistica.EmbalarEEtiquetar(admin, envioId);
    }

    [Fact]
    public async Task Manifesto_expede_os_pacotes_bipados_na_coleta()
    {
        var admin = await factory.ClienteAdminAsync();
        var (cliente, pedidoId, envioId) = await CompraNova();
        var etiqueta = await FluxoLogistica.LevarAteProntoParaEnvio(admin, envioId);

        // Fecha algum manifesto que outro teste tenha deixado aberto.
        var doca = await admin.GetFromJsonAsync<ExpedicaoDto>($"{Rota}/manifestos", Json);
        Assert.True(doca!.ProntosSemManifesto >= 1);
        foreach (var aberto in doca.Manifestos.Where(m => m.Status == StatusManifesto.Aberto))
            await admin.PostAsync($"{Rota}/manifestos/{aberto.Id}/fechar", null);

        var manifesto = await FluxoLogistica.Ler<ManifestoDto>(await admin.PostAsync($"{Rota}/manifestos", null), HttpStatusCode.Created);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsync($"{Rota}/manifestos", null)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsync($"{Rota}/manifestos/{manifesto.Id}/fechar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await admin.PostAsJsonAsync($"{Rota}/manifestos/{manifesto.Id}/pacotes", new BipagemRequest { Codigo = "SC000000000BR" }, Json)).StatusCode);

        var comPacote = await FluxoLogistica.Ler<ManifestoDto>(await admin.PostAsJsonAsync($"{Rota}/manifestos/{manifesto.Id}/pacotes",
            new BipagemRequest { Codigo = etiqueta.CodigoRastreio.ToLowerInvariant() }, Json));
        Assert.Single(comPacote.Pacotes);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await admin.PostAsJsonAsync($"{Rota}/manifestos/{manifesto.Id}/pacotes", new BipagemRequest { Codigo = etiqueta.CodigoRastreio }, Json)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsync($"{Rota}/manifestos/{manifesto.Id}/coletar", null)).StatusCode);

        // Tirar e pôr de volta, antes de fechar.
        await FluxoLogistica.Ler<ManifestoDto>(await admin.DeleteAsync($"{Rota}/manifestos/{manifesto.Id}/pacotes/{envioId}"));
        await admin.PostAsJsonAsync($"{Rota}/manifestos/{manifesto.Id}/pacotes", new BipagemRequest { Codigo = etiqueta.CodigoRastreio }, Json);
        await FluxoLogistica.Ler<ManifestoDto>(await admin.PostAsync($"{Rota}/manifestos/{manifesto.Id}/fechar", null));
        var coletado = await FluxoLogistica.Ler<ManifestoDto>(await admin.PostAsync($"{Rota}/manifestos/{manifesto.Id}/coletar", null));

        Assert.Equal(StatusManifesto.Coletado, coletado.Status);
        var envio = await FluxoLogistica.EnvioDoPedido(cliente, pedidoId);
        Assert.Equal(StatusEnvio.Expedido, envio.Status);
        Assert.Contains("Correio Simulado", envio.Eventos.Last().Descricao);
        var resumo = (await admin.GetFromJsonAsync<ExpedicaoDto>($"{Rota}/manifestos", Json))!.Manifestos.Single(m => m.Id == manifesto.Id);
        Assert.Equal(1, resumo.Volumes);
        Assert.True(resumo.PesoGramas > 0);
    }

    [Fact]
    public async Task Estoque_move_o_produto_de_prateleira()
    {
        var admin = await factory.ClienteAdminAsync();

        var movido = await FluxoLogistica.Ler<PosicaoEstoqueDto>(await admin.PutAsJsonAsync($"{Rota}/estoque/2/local", new MoverProdutoRequest { Local = "d-09-3" }, Json));

        Assert.Equal("D-09-3", movido.Local);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await admin.PutAsJsonAsync($"{Rota}/estoque/2/local", new MoverProdutoRequest { Local = "fundo" }, Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await admin.PutAsJsonAsync($"{Rota}/estoque/999/local", new MoverProdutoRequest { Local = "A-01-1" }, Json)).StatusCode);
    }

    [Fact]
    public async Task Produto_aceita_sku_e_medidas_e_recusa_sku_repetido()
    {
        var admin = await factory.ClienteAdminAsync();

        var criado = await FluxoLogistica.Ler<ProdutoDto>(await admin.PostAsJsonAsync("/api/v1/produtos", new SalvarProdutoRequest
        {
            Nome = "Garrafa térmica", Preco = 89.9m, Estoque = 5, Sku = "gar-0100", PesoGramas = 600, AlturaCm = 30, LarguraCm = 9, ComprimentoCm = 9,
        }, Json), HttpStatusCode.Created);
        Assert.Equal("GAR-0100", criado.Sku);
        Assert.Equal(30, criado.AlturaCm);

        var repetido = await admin.PostAsJsonAsync("/api/v1/produtos", new SalvarProdutoRequest { Nome = "Outra garrafa", Preco = 10m, Sku = "CAN-0005" }, Json);
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);

        var semSku = await FluxoLogistica.Ler<ProdutoDto>(await admin.PutAsJsonAsync($"/api/v1/produtos/{criado.Id}",
            new SalvarProdutoRequest { Nome = "Garrafa térmica 1 L", Preco = 99.9m, Estoque = 5 }, Json));
        Assert.Equal("GAR-0100", semSku.Sku);
        Assert.Equal(600, semSku.PesoGramas);
    }

    [Fact]
    public async Task Entregador_consulta_mas_nao_opera_o_cd()
    {
        var entregador = await _fluxo.ClienteComPapel(PapelUsuario.Entregador);

        Assert.Equal(HttpStatusCode.OK, (await entregador.GetAsync($"{Rota}/ondas")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await entregador.PostAsync($"{Rota}/manifestos", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await entregador.PostAsJsonAsync($"{Rota}/estoque/1/inventario", new InventarioRequest { QuantidadeContada = 1 }, Json)).StatusCode);
    }
}
