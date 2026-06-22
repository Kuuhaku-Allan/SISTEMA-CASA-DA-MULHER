using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaMulher.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRecuperacaoSeguranca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SecuritySetupRequired",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RecuperacaoSegurancaTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FuncionarioId = table.Column<string>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EmailDestino = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpSolicitante = table.Column<string>(type: "TEXT", nullable: false),
                    UserAgent = table.Column<string>(type: "TEXT", nullable: false),
                    Tentativas = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecuperacaoSegurancaTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecuperacaoSegurancaTokens_ExpiraEm",
                table: "RecuperacaoSegurancaTokens",
                column: "ExpiraEm");

            migrationBuilder.CreateIndex(
                name: "IX_RecuperacaoSegurancaTokens_FuncionarioId",
                table: "RecuperacaoSegurancaTokens",
                column: "FuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RecuperacaoSegurancaTokens_TokenHash",
                table: "RecuperacaoSegurancaTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecuperacaoSegurancaTokens");

            migrationBuilder.DropColumn(
                name: "SecuritySetupRequired",
                table: "AspNetUsers");
        }
    }
}
