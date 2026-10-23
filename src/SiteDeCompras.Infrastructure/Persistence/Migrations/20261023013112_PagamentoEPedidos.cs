using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteDeCompras.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PagamentoEPedidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DescontoPix",
                table: "Produtos",
                type: "REAL",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 5.0);

            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Pagamento_Forma = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Pagamento_Subtotal = table.Column<double>(type: "REAL", precision: 18, scale: 2, nullable: false),
                    Pagamento_DescontoPercentual = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    Pagamento_Desconto = table.Column<double>(type: "REAL", precision: 18, scale: 2, nullable: false),
                    Pagamento_TaxaJurosMensal = table.Column<double>(type: "REAL", precision: 9, scale: 6, nullable: false),
                    Pagamento_Juros = table.Column<double>(type: "REAL", precision: 18, scale: 2, nullable: false),
                    Pagamento_Total = table.Column<double>(type: "REAL", precision: 18, scale: 2, nullable: false),
                    Pagamento_Parcelas = table.Column<int>(type: "INTEGER", nullable: false),
                    Pagamento_ValorParcela = table.Column<double>(type: "REAL", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItensPedido",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    NomeProduto = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    ImagemUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PrecoUnitario = table.Column<double>(type: "REAL", precision: 18, scale: 2, nullable: false),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensPedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensPedido_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensPedido_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 1,
                column: "DescontoPix",
                value: 10.0);

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 2,
                column: "DescontoPix",
                value: 8.0);

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 3,
                column: "DescontoPix",
                value: 12.0);

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 4,
                column: "DescontoPix",
                value: 15.0);

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 5,
                column: "DescontoPix",
                value: 5.0);

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 6,
                column: "DescontoPix",
                value: 7.0);

            migrationBuilder.CreateIndex(
                name: "IX_ItensPedido_PedidoId",
                table: "ItensPedido",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensPedido_ProdutoId",
                table: "ItensPedido",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_CriadoEm",
                table: "Pedidos",
                column: "CriadoEm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensPedido");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropColumn(
                name: "DescontoPix",
                table: "Produtos");
        }
    }
}
