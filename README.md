# Site de compras - backend

API REST em **ASP.NET Core 8** com **EF Core** (SQLite em desenvolvimento) para o [site de compras](https://github.com/ArthurTech8/site_de_compras).

## Como rodar

Requer o [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet run --project src/SiteDeCompras.Api
```

Para ter um administrador (necessário para criar, editar e remover produtos), defina as credenciais
fora do código, com user-secrets ou variáveis de ambiente. Ele é criado na inicialização se ainda
não existir nenhum admin:

```bash
cd src/SiteDeCompras.Api
dotnet user-secrets set "AdminInicial:Email" "voce@exemplo.com"
dotnet user-secrets set "AdminInicial:Senha" "uma-senha-longa-que-so-voce-sabe"
```

A API sobe em `http://localhost:5285`, com o Swagger em `http://localhost:5285/swagger`.
Em desenvolvimento o banco `site_de_compras.db` é criado e populado sozinho com os 6 produtos do site.

```bash
dotnet test   # testes de domínio e de integração da API
```

## Arquitetura

```
src/
  SiteDeCompras.Domain          Entidades e regras de negócio (Produto, Carrinho). Sem dependências.
  SiteDeCompras.Application     Casos de uso (services), DTOs e interfaces de repositório.
  SiteDeCompras.Infrastructure  EF Core: DbContext, mapeamentos, migrations e repositórios.
  SiteDeCompras.Api             Controllers, tratamento de erros, Swagger, CORS e health check.
tests/
  SiteDeCompras.Tests           xUnit: testes de domínio e de integração (WebApplicationFactory).
```

As dependências apontam para dentro (Api → Infrastructure → Application → Domain), então trocar
o banco ou expor outra interface (fila, gRPC) não mexe nas regras de negócio.

## Rotas (v1)

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/v1/produtos` | Lista paginada. Query: `busca`, `precoMin`, `precoMax`, `ordenacao` (`Nome`, `MenorPreco`, `MaiorPreco`, `MaisRecentes`), `pagina`, `tamanhoPagina` |
| GET | `/api/v1/produtos/{id}` | Detalhe do produto |
| POST | `/api/v1/produtos` | Cria produto (**admin**) |
| PUT | `/api/v1/produtos/{id}` | Atualiza produto (**admin**) |
| DELETE | `/api/v1/produtos/{id}` | Desativa produto, exclusão lógica (**admin**) |
| POST | `/api/v1/auth/cadastro` | Cria conta de cliente `{ nome, email, senha }` e abre a sessão |
| POST | `/api/v1/auth/login` | Abre a sessão `{ email, senha }` |
| POST | `/api/v1/auth/logout` | Encerra a sessão |
| GET | `/api/v1/conta` | Dados de quem está logado (`401` sem sessão) |
| GET | `/api/v1/conta/pedidos` | Pedidos da conta, do mais recente para o mais antigo |
| GET / POST | `/api/v1/conta/enderecos` | Endereços salvos da conta (até 10) |
| DELETE | `/api/v1/conta/enderecos/{id}` | Apaga um endereço salvo |
| POST | `/api/v1/carrinhos` | Cria carrinho vazio e devolve o `id` |
| GET | `/api/v1/carrinhos/{id}` | Itens, quantidade total e valor total |
| DELETE | `/api/v1/carrinhos/{id}` | Exclui o carrinho |
| POST | `/api/v1/carrinhos/{id}/itens` | Adiciona produto `{ produtoId, quantidade }` (soma se já existir) |
| PUT | `/api/v1/carrinhos/{id}/itens/{produtoId}` | Altera a quantidade `{ quantidade }` |
| DELETE | `/api/v1/carrinhos/{id}/itens/{produtoId}` | Remove o item |
| DELETE | `/api/v1/carrinhos/{id}/itens` | Esvazia o carrinho |
| GET | `/api/v1/carrinhos/{id}/pagamento` | Formas de pagamento calculadas sobre o total do carrinho (Pix, débito e crédito de 6x a 16x) |
| POST | `/api/v1/pedidos` | Finaliza o carrinho `{ carrinhoId, formaPagamento, parcelas, enderecoEntrega }` com pagamento simulado e abre o envio |
| GET | `/api/v1/pedidos/{id}` | Detalhe do pedido. Pedido feito com login só aparece para o dono ou um admin |
| GET | `/api/v1/pedidos/{id}/rastreio` | Envios do pedido com o histórico de cada um (mesma regra de acesso) |
| GET | `/api/v1/rastreio/{codigo}` | Rastreio público: só cidade de destino, previsão e eventos |
| GET | `/api/v1/admin/logistica/painel` | Envios por etapa, atrasados, despachos que vencem hoje e entregas de hoje (**equipe**) |
| GET | `/api/v1/admin/logistica/envios` | Fila de envios. Query: `status`, `atrasados`, `busca`, `pagina`, `tamanhoPagina` (**equipe**) |
| GET | `/api/v1/admin/logistica/envios/{id}` | Detalhe do envio (**equipe**) |
| POST | `/api/v1/admin/logistica/envios/{id}/avancar` | Leva o envio para a próxima etapa do transporte, depois da coleta; o pedido acompanha (**admin, operador do CD, entregador**) |
| GET | `/api/v1/admin/logistica/estoque` | Prateleira, disponível, reservado e divergências de cada produto (**equipe**) |
| PUT | `/api/v1/admin/logistica/estoque/{produtoId}/local` | Muda o produto de prateleira (`A-03-2`) (**CD**) |
| POST | `/api/v1/admin/logistica/estoque/{produtoId}/inventario` | Registra a contagem e resolve a divergência (**CD**) |
| GET | `/api/v1/admin/logistica/ondas` | Ondas abertas e as últimas concluídas (**equipe**) |
| POST | `/api/v1/admin/logistica/ondas` | Gera uma onda com os envios mais urgentes. Corpo opcional: `{ "envios": 10 }` (**CD**) |
| GET | `/api/v1/admin/logistica/ondas/{id}` | Rota de coleta e envios da onda (**equipe**) |
| POST | `/api/v1/admin/logistica/ondas/{id}/bipar` | Bipe de um SKU: `{ "codigo": "CAN-0005" }` (**CD**) |
| POST | `/api/v1/admin/logistica/ondas/{id}/falta` | Produto ausente: `{ "produtoId": 5 }`. Os envios voltam para a fila (**CD**) |
| POST | `/api/v1/admin/logistica/ondas/{id}/concluir` | Fecha a onda quando tudo foi separado (**CD**) |
| GET | `/api/v1/admin/logistica/envios/{id}/embalagem` | Itens a conferir, caixa sugerida e pesos esperados (**equipe**) |
| POST | `/api/v1/admin/logistica/envios/{id}/conferir` | Bipe de um SKU na bancada (**CD**) |
| POST | `/api/v1/admin/logistica/envios/{id}/embalar` | `{ "caixa": "M", "pesoGramas": 1180 }` (**CD**) |
| POST/GET | `/api/v1/admin/logistica/envios/{id}/etiqueta` | Gera a etiqueta e o código de rastreio / lê a etiqueta (**CD** / **equipe**) |
| GET | `/api/v1/admin/logistica/manifestos` | Manifestos na doca, últimos coletados e pacotes prontos fora de manifesto (**equipe**) |
| POST | `/api/v1/admin/logistica/manifestos` | Abre o manifesto da transportadora (**CD**) |
| GET | `/api/v1/admin/logistica/manifestos/{id}` | Pacotes do manifesto (**equipe**) |
| POST | `/api/v1/admin/logistica/manifestos/{id}/pacotes` | Bipe da etiqueta: `{ "codigo": "SC123456789BR" }` (**CD**) |
| DELETE | `/api/v1/admin/logistica/manifestos/{id}/pacotes/{envioId}` | Tira o pacote do manifesto aberto (**CD**) |
| POST | `/api/v1/admin/logistica/manifestos/{id}/fechar` | Fecha o manifesto (**CD**) |
| POST | `/api/v1/admin/logistica/manifestos/{id}/coletar` | Coleta confirmada: todos os pacotes são expedidos (**CD**) |
| GET | `/health` | Health check (inclui o banco) |

Erros seguem o padrão **ProblemDetails** (`application/problem+json`):
`400` validação, `401` sem sessão ou login recusado, `403` sem permissão, `404` recurso inexistente,
`409` e-mail já cadastrado, `422` regra de negócio (ex.: estoque insuficiente), `429` limite de requisições.

## Logística

Cada pedido pago abre um **envio**, separado do pedido como no Mercado Envios, para que um pedido
possa gerar vários envios no futuro. O envio passa por `AguardandoSeparacao`, `EmSeparacao`,
`Separado`, `Embalado`, `ProntoParaEnvio` (quando nasce o código de rastreio `SC000000000BR`),
`Expedido`, `EmTransito`, `SaiuParaEntrega` e `Entregue`, e cada passo grava um evento de rastreio.
O pedido acompanha: `Pago`, `EmPreparacao`, `Enviado` e `Entregue`.

- **Prazo de despacho**: pago até 12h (Brasília) em dia útil, sai no mesmo dia às 18h; depois, no
  próximo dia útil.
- **Previsão de entrega**: o prazo de despacho mais os dias úteis de transporte da região do CEP
  (tabela fictícia em `CalendarioLogistica`).
- **Equipe**: os papéis `OperadorCD`, `Entregador` e `Atendimento` se juntam a `Admin`. Todos
  consultam a logística. **CD** nas rotas acima quer dizer admin ou operador do CD; o entregador só
  avança o transporte, e o atendimento só consulta.

### Centro de distribuição

Dentro do CD o envio anda pelas telas, não pelo "avançar":

1. **Separação em ondas**: a onda junta os envios mais urgentes (menor prazo de despacho) e monta a
   rota de coleta pela prateleira (`LocalEstoque`, corredor-módulo-nível). Cada bipe de SKU vai para o
   envio mais urgente que ainda precisa do produto; com a última unidade, o envio fica `Separado`.
   Produto ausente: os envios dele voltam para a fila com um evento interno (o cliente não vê) e abre
   uma **divergência de estoque**. Até o inventário, esses envios não entram em ondas.
2. **Inventário**: a contagem da prateleira menos as unidades vendidas e ainda não separadas vira o
   disponível para venda. Se a contagem cobre os pedidos, a divergência é resolvida.
3. **Embalagem**: o embalador bipa cada unidade, o sistema sugere a menor caixa (P, M, G, GG) onde
   tudo cabe com 15% de folga, ou volume avulso para produto grande, e a balança precisa ficar a até
   20% do peso esperado (produtos + caixa). Depois, a etiqueta gera o código de rastreio.
4. **Expedição**: as etiquetas bipadas entram no manifesto da transportadora (por enquanto só a
   `Correio Simulado`). Fechado o manifesto, a coleta expede todos os pacotes de uma vez.

O produto ganhou `Sku` (único), `PesoGramas`, `AlturaCm`, `LarguraCm`, `ComprimentoCm` e
`LocalEstoque`, que entram opcionalmente no `POST`/`PUT` de produtos. A migration
`CentroDistribuicao` copia os itens dos pedidos para os envios que já existiam e devolve à fila os
que estavam `EmSeparacao` sem onda.

## Segurança

- **Senhas**: PBKDF2-HMAC-SHA512 com 210.000 iterações e sal por senha (hasher do ASP.NET Core
  Identity, recomendação da OWASP). Mínimo de 8 e máximo de 128 caracteres, recusando senhas comuns
  e senhas com o nome ou o e-mail. Nada sobre a senha, o hash ou o bloqueio sai nas respostas.
- **E-mail**: único sem diferenciar maiúsculas ou espaços (`EmailNormalizado` com índice único).
- **Login**: a mesma mensagem para e-mail inexistente, senha errada e conta bloqueada, com o mesmo
  custo de tempo. Cinco senhas erradas seguidas bloqueiam a conta por 15 minutos.
- **Sessão**: cookie `HttpOnly`, `SameSite=Strict` e, fora do desenvolvimento, `Secure`. O JavaScript
  não lê o cookie, então um XSS não rouba a sessão. Trocar a senha ou o papel derruba as sessões abertas.
- **Papéis**: o cadastro sempre cria `Cliente`; o primeiro `Admin` vem de `AdminInicial` na configuração.
  O papel é gravado como texto, então novos papéis entram sem migração de dados.
- **CSRF**: além do `SameSite=Strict`, todo `POST`/`PUT`/`DELETE` em `/api` exige o cabeçalho
  `X-Requested-With`, que outro site não consegue mandar sem passar pelo CORS.
- **Limite de requisições** por IP: 300/min no geral e 10/min em login e cadastro
  (`LimiteRequisicoes` no `appsettings.json`). Atrás de proxy reverso, configure `ForwardedHeaders`
  para o limite enxergar o IP real.
- **Cabeçalhos**: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, CSP restrita e
  `Cache-Control: no-store` na API; HSTS e redirecionamento para HTTPS fora do desenvolvimento.
- **Produção**: sirva o site e a API no mesmo domínio, persista as chaves do Data Protection
  (senão as sessões caem a cada deploy) e defina `AllowedHosts` com o domínio real.

## Pagamento (simulado)

Não há gateway: os valores são calculados no servidor a partir do carrinho e o pedido é aprovado na hora.
O cliente só escolhe a forma de pagamento e, no crédito, o número de parcelas.

- **Pix**: cada produto tem um desconto próprio entre 5% e 15% (`descontoPix`). Os descontos não se somam:
  vale o maior entre os produtos do carrinho, aplicado uma vez sobre o total.
- **Débito**: o total do carrinho, sem desconto nem juros.
- **Crédito**: de 6 a 16 parcelas fixas com juros compostos pela Tabela Price. A taxa padrão é
  **2,99% ao mês** (`Pagamento:TaxaJurosMensalCredito` no `appsettings.json`), dentro da faixa que o
  varejo online costuma cobrar no parcelado com juros. A resposta traz o valor de cada parcela, o total
  e quanto ele fica acima do total do carrinho.

Ao finalizar, o pedido guarda uma cópia dos itens e da condição de pagamento, o estoque é baixado e o
carrinho é esvaziado.

## Migrations

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet ef migrations add NomeDaMudanca -p src/SiteDeCompras.Infrastructure -s src/SiteDeCompras.Api -o Persistence/Migrations
```

## Próximos passos conhecidos

- As rotas de escrita de produto ainda não têm autenticação; entram junto com usuários/admin.
- SQLite é só para desenvolvimento. Em produção, trocar por PostgreSQL ou SQL Server
  (e remover a conversão de `decimal` para `double` em `AppDbContext`).
