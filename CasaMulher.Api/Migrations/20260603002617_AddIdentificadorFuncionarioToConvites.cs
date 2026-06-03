using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaMulher.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentificadorFuncionarioToConvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentificadorFuncionario",
                table: "FuncionariosConvites",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                WITH convite_base AS (
                    SELECT
                        "Id",
                        CASE lower("Perfil")
                            WHEN 'adm' THEN 'ADM'
                            WHEN 'recepcao' THEN 'REC'
                            WHEN 'professor' THEN 'PRO'
                            WHEN 'as_social' THEN 'SOC'
                            WHEN 'juridico' THEN 'JUR'
                            ELSE 'FUN'
                        END AS prefixo,
                        row_number() OVER (PARTITION BY lower("Perfil") ORDER BY "CriadoEm", "Id") AS rn
                    FROM "FuncionariosConvites"
                    WHERE "IdentificadorFuncionario" = ''
                ),
                maximos AS (
                    SELECT prefixo, max(numero) AS maior_numero
                    FROM (
                        SELECT
                            substr("IdentificadorFuncionario", 1, instr("IdentificadorFuncionario", '-') - 1) AS prefixo,
                            CAST(substr("IdentificadorFuncionario", instr("IdentificadorFuncionario", '-') + 1) AS INTEGER) AS numero
                        FROM "AspNetUsers"
                        WHERE "IdentificadorFuncionario" LIKE '%-%'
                    )
                    GROUP BY prefixo
                )
                UPDATE "FuncionariosConvites"
                SET "IdentificadorFuncionario" = (
                    SELECT
                        convite_base.prefixo || '-' || printf('%06d', coalesce(maximos.maior_numero, 0) + convite_base.rn)
                    FROM convite_base
                    LEFT JOIN maximos ON maximos.prefixo = convite_base.prefixo
                    WHERE convite_base."Id" = "FuncionariosConvites"."Id"
                )
                WHERE "IdentificadorFuncionario" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_FuncionariosConvites_IdentificadorFuncionario",
                table: "FuncionariosConvites",
                column: "IdentificadorFuncionario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FuncionariosConvites_IdentificadorFuncionario",
                table: "FuncionariosConvites");

            migrationBuilder.DropColumn(
                name: "IdentificadorFuncionario",
                table: "FuncionariosConvites");
        }
    }
}
