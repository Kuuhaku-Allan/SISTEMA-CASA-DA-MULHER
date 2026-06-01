using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaMulher.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoriaEventos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditoriaEventos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    IdentificadorFuncionario = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NomeFuncionario = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    PerfilFuncionario = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Acao = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Entidade = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    EntidadeId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IpOrigem = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaEventos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaEventos_CriadoEm",
                table: "AuditoriaEventos",
                column: "CriadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaEventos_EntidadeId",
                table: "AuditoriaEventos",
                column: "EntidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaEventos_UsuarioId",
                table: "AuditoriaEventos",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditoriaEventos");
        }
    }
}
