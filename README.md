# Backend — Site de Compras

API REST em **ASP.NET Core 8** com **EF Core** (SQLite em desenvolvimento).

## Como rodar

Requer o [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
cd backend
dotnet run --project src/SiteDeCompras.Api
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
| POST | `/api/v1/produtos` | Cria produto |
| PUT | `/api/v1/produtos/{id}` | Atualiza produto |
| DELETE | `/api/v1/produtos/{id}` | Desativa produto (exclusão lógica) |
| POST | `/api/v1/carrinhos` | Cria carrinho vazio e devolve o `id` |
| GET | `/api/v1/carrinhos/{id}` | Itens, quantidade total e valor total |
| DELETE | `/api/v1/carrinhos/{id}` | Exclui o carrinho |
| POST | `/api/v1/carrinhos/{id}/itens` | Adiciona produto `{ produtoId, quantidade }` (soma se já existir) |
| PUT | `/api/v1/carrinhos/{id}/itens/{produtoId}` | Altera a quantidade `{ quantidade }` |
| DELETE | `/api/v1/carrinhos/{id}/itens/{produtoId}` | Remove o item |
| DELETE | `/api/v1/carrinhos/{id}/itens` | Esvazia o carrinho |
| GET | `/health` | Health check (inclui o banco) |

Erros seguem o padrão **ProblemDetails** (`application/problem+json`):
`400` validação, `404` recurso inexistente, `422` regra de negócio (ex.: estoque insuficiente).

## Migrations

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet ef migrations add NomeDaMudanca -p src/SiteDeCompras.Infrastructure -s src/SiteDeCompras.Api -o Persistence/Migrations
```

## Próximos passos conhecidos

- As rotas de escrita de produto ainda não têm autenticação; entram junto com usuários/admin.
- SQLite é só para desenvolvimento. Em produção, trocar por PostgreSQL ou SQL Server
  (e remover a conversão de `decimal` para `double` em `AppDbContext`).
