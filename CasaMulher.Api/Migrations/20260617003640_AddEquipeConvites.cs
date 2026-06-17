using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaMulher.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipeConvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EquipeConvites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodigoEquipe = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CodigoAtivacaoHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CriadoPorUserId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    UsadoPorUserId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    NomeInformado = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    PapelEquipe = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    PrecisaFork = table.Column<bool>(type: "INTEGER", nullable: false),
                    PodeCriarConvitesEquipe = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevogadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipeConvites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EquipeMembros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CodigoEquipe = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    PapelEquipe = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    PrecisaFork = table.Column<bool>(type: "INTEGER", nullable: false),
                    PodeCriarConvitesEquipe = table.Column<bool>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipeMembros", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipeConvites_CodigoAtivacaoHash",
                table: "EquipeConvites",
                column: "CodigoAtivacaoHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipeConvites_CodigoEquipe",
                table: "EquipeConvites",
                column: "CodigoEquipe",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipeConvites_Status",
                table: "EquipeConvites",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EquipeMembros_CodigoEquipe",
                table: "EquipeMembros",
                column: "CodigoEquipe",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipeMembros_UserId",
                table: "EquipeMembros",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipeConvites");

            migrationBuilder.DropTable(
                name: "EquipeMembros");
        }
    }
}
