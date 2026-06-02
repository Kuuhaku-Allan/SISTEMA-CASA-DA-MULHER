using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaMulher.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailEventos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailEventos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Destinatario = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Assunto = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Erro = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailEventos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailEventos_CriadoEm",
                table: "EmailEventos",
                column: "CriadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_EmailEventos_Destinatario",
                table: "EmailEventos",
                column: "Destinatario");

            migrationBuilder.CreateIndex(
                name: "IX_EmailEventos_Status",
                table: "EmailEventos",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailEventos_Tipo",
                table: "EmailEventos",
                column: "Tipo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailEventos");
        }
    }
}
