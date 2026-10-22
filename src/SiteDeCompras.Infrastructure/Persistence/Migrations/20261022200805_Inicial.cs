using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SiteDeCompras.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Carrinhos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carrinhos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Preco = table.Column<double>(type: "REAL", precision: 18, scale: 2, nullable: false),
                    ImagemUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Estoque = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItensCarrinho",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CarrinhoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    NomeProduto = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    ImagemUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PrecoUnitario = table.Column<double>(type: "REAL", precision: 18, scale: 2, nullable: false),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensCarrinho", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensCarrinho_Carrinhos_CarrinhoId",
                        column: x => x.CarrinhoId,
                        principalTable: "Carrinhos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensCarrinho_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Produtos",
                columns: new[] { "Id", "Ativo", "AtualizadoEm", "CriadoEm", "Descricao", "Estoque", "ImagemUrl", "Nome", "Preco" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, "imagens/Geladeira.webp", "Geladeira", 2300.0 },
                    { 2, true, new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, "imagens/sofa3lugares.webp", "Sofá 3 lugares", 2459.0 },
                    { 3, true, new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, "imagens/celularxaiomi.webp", "Celular Xiaomi", 2189.9000000000001 },
                    { 4, true, new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, "imagens/notebook.webp", "Notebook", 4589.0 },
                    { 5, true, new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, "imagens/caneca.webp", "Caneca", 59.0 },
                    { 6, true, new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, "imagens/ps4.webp", "Playstation 4", 2229.8899999999999 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensCarrinho_CarrinhoId_ProdutoId",
                table: "ItensCarrinho",
                columns: new[] { "CarrinhoId", "ProdutoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensCarrinho_ProdutoId",
                table: "ItensCarrinho",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Ativo",
                table: "Produtos",
                column: "Ativo");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Nome",
                table: "Produtos",
                column: "Nome");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensCarrinho");

            migrationBuilder.DropTable(
                name: "Carrinhos");

            migrationBuilder.DropTable(
                name: "Produtos");
        }
    }
}
