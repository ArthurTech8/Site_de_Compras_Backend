using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteDeCompras.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CentroDistribuicao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AlturaCm",
                table: "Produtos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "ComprimentoCm",
                table: "Produtos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "LarguraCm",
                table: "Produtos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<string>(
                name: "LocalEstoque",
                table: "Produtos",
                type: "TEXT",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PesoGramas",
                table: "Produtos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 500);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "Produtos",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Publico",
                table: "EventosRastreio",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ManifestoId",
                table: "Envios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OndaId",
                table: "Envios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PacoteAlturaCm",
                table: "Envios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PacoteCaixa",
                table: "Envios",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PacoteComprimentoCm",
                table: "Envios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PacoteLarguraCm",
                table: "Envios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PacotePesoGramas",
                table: "Envios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Transportadora",
                table: "Envios",
                type: "TEXT",
                maxLength: 60,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DivergenciasEstoque",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    OndaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvidaEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DivergenciasEstoque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DivergenciasEstoque_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensEnvio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnvioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    NomeProduto = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantidadeSeparada = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantidadeConferida = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensEnvio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensEnvio_Envios_EnvioId",
                        column: x => x.EnvioId,
                        principalTable: "Envios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Manifestos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Transportadora = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ColetadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manifestos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OndasSeparacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConcluidaEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OndasSeparacao", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AlturaCm", "ComprimentoCm", "LarguraCm", "LocalEstoque", "PesoGramas", "Sku" },
                values: new object[] { 175, 72, 65, "D-01-1", 68000, "GEL-0001" });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AlturaCm", "ComprimentoCm", "LarguraCm", "LocalEstoque", "PesoGramas", "Sku" },
                values: new object[] { 90, 210, 95, "D-02-1", 52000, "SOF-0002" });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AlturaCm", "ComprimentoCm", "LarguraCm", "LocalEstoque", "PesoGramas", "Sku" },
                values: new object[] { 6, 18, 10, "A-01-2", 420, "CEL-0003" });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AlturaCm", "ComprimentoCm", "LarguraCm", "LocalEstoque", "PesoGramas", "Sku" },
                values: new object[] { 7, 42, 30, "A-02-1", 2600, "NTB-0004" });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AlturaCm", "ComprimentoCm", "LarguraCm", "LocalEstoque", "PesoGramas", "Sku" },
                values: new object[] { 11, 13, 11, "A-03-2", 450, "CAN-0005" });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "AlturaCm", "ComprimentoCm", "LarguraCm", "LocalEstoque", "PesoGramas", "Sku" },
                values: new object[] { 11, 40, 33, "B-11-1", 3400, "PS4-0006" });

            // Produtos cadastrados depois do catálogo inicial ganham um SKU antes do índice único.
            migrationBuilder.Sql("UPDATE Produtos SET Sku = 'PRD-' || printf('%05d', Id) WHERE Sku = '';");

            // Os envios da fase anterior não tinham itens: copia os do pedido. Quem já passou da
            // separação fica com tudo separado, e quem já passou da embalagem, com tudo conferido.
            migrationBuilder.Sql(@"
INSERT INTO ItensEnvio (EnvioId, ProdutoId, NomeProduto, Quantidade, QuantidadeSeparada, QuantidadeConferida)
SELECT e.Id, i.ProdutoId, i.NomeProduto, i.Quantidade,
       CASE WHEN e.Status IN ('AguardandoSeparacao', 'EmSeparacao') THEN 0 ELSE i.Quantidade END,
       CASE WHEN e.Status IN ('AguardandoSeparacao', 'EmSeparacao', 'Separado') THEN 0 ELSE i.Quantidade END
FROM Envios e JOIN ItensPedido i ON i.PedidoId = e.PedidoId;");

            // Separação manual da fase anterior, sem onda: volta para a fila e entra numa onda.
            migrationBuilder.Sql("UPDATE Envios SET Status = 'AguardandoSeparacao' WHERE Status = 'EmSeparacao';");

            // Pacotes etiquetados antes da expedição existir saem pela transportadora padrão.
            migrationBuilder.Sql("UPDATE Envios SET Transportadora = 'Correio Simulado' WHERE CodigoRastreio IS NOT NULL AND Transportadora IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Sku",
                table: "Produtos",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Envios_ManifestoId",
                table: "Envios",
                column: "ManifestoId");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_OndaId",
                table: "Envios",
                column: "OndaId");

            migrationBuilder.CreateIndex(
                name: "IX_DivergenciasEstoque_ProdutoId_ResolvidaEm",
                table: "DivergenciasEstoque",
                columns: new[] { "ProdutoId", "ResolvidaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_ItensEnvio_EnvioId",
                table: "ItensEnvio",
                column: "EnvioId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensEnvio_ProdutoId",
                table: "ItensEnvio",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Manifestos_Status",
                table: "Manifestos",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OndasSeparacao_Status",
                table: "OndasSeparacao",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Envios_Manifestos_ManifestoId",
                table: "Envios",
                column: "ManifestoId",
                principalTable: "Manifestos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Envios_OndasSeparacao_OndaId",
                table: "Envios",
                column: "OndaId",
                principalTable: "OndasSeparacao",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Envios_Manifestos_ManifestoId",
                table: "Envios");

            migrationBuilder.DropForeignKey(
                name: "FK_Envios_OndasSeparacao_OndaId",
                table: "Envios");

            migrationBuilder.DropTable(
                name: "DivergenciasEstoque");

            migrationBuilder.DropTable(
                name: "ItensEnvio");

            migrationBuilder.DropTable(
                name: "Manifestos");

            migrationBuilder.DropTable(
                name: "OndasSeparacao");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_Sku",
                table: "Produtos");

            migrationBuilder.DropIndex(
                name: "IX_Envios_ManifestoId",
                table: "Envios");

            migrationBuilder.DropIndex(
                name: "IX_Envios_OndaId",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "AlturaCm",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "ComprimentoCm",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "LarguraCm",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "LocalEstoque",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "PesoGramas",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Publico",
                table: "EventosRastreio");

            migrationBuilder.DropColumn(
                name: "ManifestoId",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "OndaId",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "PacoteAlturaCm",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "PacoteCaixa",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "PacoteComprimentoCm",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "PacoteLarguraCm",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "PacotePesoGramas",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "Transportadora",
                table: "Envios");
        }
    }
}
