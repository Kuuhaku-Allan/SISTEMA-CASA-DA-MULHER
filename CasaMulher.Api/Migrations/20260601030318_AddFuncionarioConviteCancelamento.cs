using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaMulher.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFuncionarioConviteCancelamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Cancelado",
                table: "FuncionariosConvites",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CanceladoEm",
                table: "FuncionariosConvites",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cancelado",
                table: "FuncionariosConvites");

            migrationBuilder.DropColumn(
                name: "CanceladoEm",
                table: "FuncionariosConvites");
        }
    }
}
