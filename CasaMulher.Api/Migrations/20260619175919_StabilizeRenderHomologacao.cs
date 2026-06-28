using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaMulher.Api.Migrations
{
    /// <inheritdoc />
    public partial class StabilizeRenderHomologacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RpId",
                table: "PasskeyCredentials",
                type: "TEXT",
                maxLength: 253,
                nullable: false,
                defaultValue: "localhost");

            migrationBuilder.AddColumn<string>(
                name: "Escopo",
                table: "AuditoriaEventos",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Institucional");

            migrationBuilder.Sql(
                """
                UPDATE "AuditoriaEventos"
                SET "Escopo" = 'Equipe'
                WHERE upper("IdentificadorFuncionario") LIKE 'EQP-%'
                   OR upper("Acao") LIKE 'EQUIPE_%'
                   OR upper("Descricao") LIKE '%EQP-%'
                   OR lower("PerfilFuncionario") = 'equipe';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaEventos_Escopo",
                table: "AuditoriaEventos",
                column: "Escopo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditoriaEventos_Escopo",
                table: "AuditoriaEventos");

            migrationBuilder.DropColumn(
                name: "RpId",
                table: "PasskeyCredentials");

            migrationBuilder.DropColumn(
                name: "Escopo",
                table: "AuditoriaEventos");
        }
    }
}
