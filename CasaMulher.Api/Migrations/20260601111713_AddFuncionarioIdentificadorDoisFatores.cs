using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaMulher.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFuncionarioIdentificadorDoisFatores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeveTrocarSenha",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DoisFatoresObrigatorio",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IdentificadorFuncionario",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                WITH numbered AS (
                    SELECT
                        "Id",
                        row_number() OVER (ORDER BY "CriadoEm", "Id") AS rn
                    FROM "AspNetUsers"
                )
                UPDATE "AspNetUsers"
                SET
                    "IdentificadorFuncionario" = 'CM-' || printf('%06d', (
                        SELECT rn FROM numbered WHERE numbered."Id" = "AspNetUsers"."Id"
                    )),
                    "UserName" = 'CM-' || printf('%06d', (
                        SELECT rn FROM numbered WHERE numbered."Id" = "AspNetUsers"."Id"
                    )),
                    "NormalizedUserName" = 'CM-' || printf('%06d', (
                        SELECT rn FROM numbered WHERE numbered."Id" = "AspNetUsers"."Id"
                    )),
                    "DoisFatoresObrigatorio" = CASE
                        WHEN lower("Perfil") IN ('adm', 'juridico', 'as_social') THEN 1
                        ELSE 0
                    END
                WHERE "IdentificadorFuncionario" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IdentificadorFuncionario",
                table: "AspNetUsers",
                column: "IdentificadorFuncionario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IdentificadorFuncionario",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DeveTrocarSenha",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DoisFatoresObrigatorio",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IdentificadorFuncionario",
                table: "AspNetUsers");
        }
    }
}
