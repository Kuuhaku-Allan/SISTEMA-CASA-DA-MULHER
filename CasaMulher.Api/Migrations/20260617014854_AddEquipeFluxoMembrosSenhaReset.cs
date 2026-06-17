using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaMulher.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipeFluxoMembrosSenhaReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FluxoTrabalho",
                table: "EquipeMembros",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ForkUrl",
                table: "EquipeMembros",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubId",
                table: "EquipeMembros",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubUsername",
                table: "EquipeMembros",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GitHubVinculadoEm",
                table: "EquipeMembros",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaVerificacaoGitHubEm",
                table: "EquipeMembros",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UsaCodespaces",
                table: "EquipeMembros",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FluxoTrabalho",
                table: "EquipeConvites",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "UsaCodespaces",
                table: "EquipeConvites",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EquipeSenhaResets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodigoEquipe = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CodigoHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    GeradoPorUserId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Usado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Revogado = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevogadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipeSenhaResets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipeSenhaResets_CodigoEquipe",
                table: "EquipeSenhaResets",
                column: "CodigoEquipe");

            migrationBuilder.CreateIndex(
                name: "IX_EquipeSenhaResets_CodigoHash",
                table: "EquipeSenhaResets",
                column: "CodigoHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipeSenhaResets_ExpiraEm",
                table: "EquipeSenhaResets",
                column: "ExpiraEm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipeSenhaResets");

            migrationBuilder.DropColumn(
                name: "FluxoTrabalho",
                table: "EquipeMembros");

            migrationBuilder.DropColumn(
                name: "ForkUrl",
                table: "EquipeMembros");

            migrationBuilder.DropColumn(
                name: "GitHubId",
                table: "EquipeMembros");

            migrationBuilder.DropColumn(
                name: "GitHubUsername",
                table: "EquipeMembros");

            migrationBuilder.DropColumn(
                name: "GitHubVinculadoEm",
                table: "EquipeMembros");

            migrationBuilder.DropColumn(
                name: "UltimaVerificacaoGitHubEm",
                table: "EquipeMembros");

            migrationBuilder.DropColumn(
                name: "UsaCodespaces",
                table: "EquipeMembros");

            migrationBuilder.DropColumn(
                name: "FluxoTrabalho",
                table: "EquipeConvites");

            migrationBuilder.DropColumn(
                name: "UsaCodespaces",
                table: "EquipeConvites");
        }
    }
}
