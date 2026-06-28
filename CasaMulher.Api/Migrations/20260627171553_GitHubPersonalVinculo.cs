using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaMulher.Api.Migrations
{
    /// <inheritdoc />
    public partial class GitHubPersonalVinculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GitHubOAuthStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StateHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ApplicationUserId = table.Column<string>(type: "TEXT", nullable: false),
                    ReturnUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IpSolicitante = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitHubOAuthStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GitHubUsuarioVinculos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationUserId = table.Column<string>(type: "TEXT", nullable: false),
                    GitHubUserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    GitHubLogin = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    GitHubAvatarUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    GitHubProfileUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AccessTokenEncrypted = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshTokenEncrypted = table.Column<string>(type: "TEXT", nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TokenType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Scopes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AppMode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevogadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UltimoUsoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitHubUsuarioVinculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GitHubUsuarioVinculos_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GitHubUsuarioVinculos_ApplicationUserId",
                table: "GitHubUsuarioVinculos",
                column: "ApplicationUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GitHubOAuthStates");

            migrationBuilder.DropTable(
                name: "GitHubUsuarioVinculos");
        }
    }
}
