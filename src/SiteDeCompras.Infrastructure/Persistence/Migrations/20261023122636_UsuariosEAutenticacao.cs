using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteDeCompras.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UsuariosEAutenticacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Pedidos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    EmailNormalizado = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    SenhaHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Papel = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CarimboSeguranca = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FalhasLogin = table.Column<int>(type: "INTEGER", nullable: false),
                    BloqueadoAte = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UltimoLoginEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_UsuarioId",
                table: "Pedidos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmailNormalizado",
                table: "Usuarios",
                column: "EmailNormalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Papel",
                table: "Usuarios",
                column: "Papel");

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Usuarios_UsuarioId",
                table: "Pedidos",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Usuarios_UsuarioId",
                table: "Pedidos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_UsuarioId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Pedidos");
        }
    }
}
