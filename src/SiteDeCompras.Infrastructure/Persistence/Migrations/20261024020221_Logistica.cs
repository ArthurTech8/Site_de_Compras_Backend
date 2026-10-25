using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteDeCompras.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Logistica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Bairro",
                table: "Pedidos",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Cep",
                table: "Pedidos",
                type: "TEXT",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Cidade",
                table: "Pedidos",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Complemento",
                table: "Pedidos",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Destinatario",
                table: "Pedidos",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Logradouro",
                table: "Pedidos",
                type: "TEXT",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Numero",
                table: "Pedidos",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Uf",
                table: "Pedidos",
                type: "TEXT",
                maxLength: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EnderecosSalvos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Endereco_Destinatario = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Endereco_Cep = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Endereco_Logradouro = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Endereco_Numero = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Endereco_Complemento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Endereco_Bairro = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Endereco_Cidade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Endereco_Uf = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnderecosSalvos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnderecosSalvos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Envios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodigoRastreio = table.Column<string>(type: "TEXT", maxLength: 13, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Destino_Destinatario = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Destino_Cep = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Destino_Logradouro = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Destino_Numero = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Destino_Complemento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Destino_Bairro = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Destino_Cidade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Destino_Uf = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PrazoDespacho = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PrevisaoEntrega = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Envios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Envios_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventosRastreio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnvioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Local = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosRastreio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosRastreio_Envios_EnvioId",
                        column: x => x.EnvioId,
                        principalTable: "Envios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnderecosSalvos_UsuarioId",
                table: "EnderecosSalvos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_CodigoRastreio",
                table: "Envios",
                column: "CodigoRastreio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Envios_PedidoId",
                table: "Envios",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_PrazoDespacho",
                table: "Envios",
                column: "PrazoDespacho");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_Status",
                table: "Envios",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EventosRastreio_EnvioId",
                table: "EventosRastreio",
                column: "EnvioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnderecosSalvos");

            migrationBuilder.DropTable(
                name: "EventosRastreio");

            migrationBuilder.DropTable(
                name: "Envios");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Bairro",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Cep",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Cidade",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Complemento",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Destinatario",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Logradouro",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Numero",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Uf",
                table: "Pedidos");
        }
    }
}
