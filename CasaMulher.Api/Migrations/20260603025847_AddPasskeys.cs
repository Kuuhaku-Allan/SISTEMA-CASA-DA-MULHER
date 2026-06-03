using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaMulher.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPasskeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PasskeyReconfirmadoEm",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PasskeyChallenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChallengeId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ChallengeBytes = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OptionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiracaoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasskeyChallenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasskeyCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CredentialId = table.Column<byte[]>(type: "BLOB", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    SignatureCounter = table.Column<uint>(type: "INTEGER", nullable: false),
                    NomeDispositivo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Transports = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UltimoUsoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasskeyCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasskeyCredentials_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasskeyReconfirmacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReconfirmacaoId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CredentialId = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiracaoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasskeyReconfirmacoes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyChallenges_ChallengeId",
                table: "PasskeyChallenges",
                column: "ChallengeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyChallenges_ExpiracaoEm",
                table: "PasskeyChallenges",
                column: "ExpiracaoEm");

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyCredentials_CredentialId",
                table: "PasskeyCredentials",
                column: "CredentialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyCredentials_UserId",
                table: "PasskeyCredentials",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyReconfirmacoes_ExpiracaoEm",
                table: "PasskeyReconfirmacoes",
                column: "ExpiracaoEm");

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyReconfirmacoes_ReconfirmacaoId",
                table: "PasskeyReconfirmacoes",
                column: "ReconfirmacaoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasskeyChallenges");

            migrationBuilder.DropTable(
                name: "PasskeyCredentials");

            migrationBuilder.DropTable(
                name: "PasskeyReconfirmacoes");

            migrationBuilder.DropColumn(
                name: "PasskeyReconfirmadoEm",
                table: "AspNetUsers");
        }
    }
}
