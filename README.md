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
| POST | `/api/v1/carrinhos` | Cria carrinho vazio e devolve o `id` |
| GET | `/api/v1/carrinhos/{id}` | Itens, quantidade total e valor total |
| DELETE | `/api/v1/carrinhos/{id}` | Exclui o carrinho |
| POST | `/api/v1/carrinhos/{id}/itens` | Adiciona produto `{ produtoId, quantidade }` (soma se já existir) |
| PUT | `/api/v1/carrinhos/{id}/itens/{produtoId}` | Altera a quantidade `{ quantidade }` |
| DELETE | `/api/v1/carrinhos/{id}/itens/{produtoId}` | Remove o item |
| DELETE | `/api/v1/carrinhos/{id}/itens` | Esvazia o carrinho |
| GET | `/api/v1/carrinhos/{id}/pagamento` | Formas de pagamento calculadas sobre o total do carrinho (Pix, débito e crédito de 6x a 16x) |
| POST | `/api/v1/pedidos` | Finaliza o carrinho `{ carrinhoId, formaPagamento, parcelas }` com pagamento simulado |
| GET | `/api/v1/pedidos/{id}` | Detalhe do pedido. Pedido feito com login só aparece para o dono ou um admin |
| GET | `/health` | Health check (inclui o banco) |

Erros seguem o padrão **ProblemDetails** (`application/problem+json`):
`400` validação, `401` sem sessão ou login recusado, `403` sem permissão, `404` recurso inexistente,
`409` e-mail já cadastrado, `422` regra de negócio (ex.: estoque insuficiente), `429` limite de requisições.

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
