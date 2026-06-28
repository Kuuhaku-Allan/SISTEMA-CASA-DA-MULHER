using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaMulher.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPasskeyOriginAndEnv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedEnvironment",
                table: "PasskeyCredentials",
                type: "TEXT",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "PasskeyCredentials",
                type: "TEXT",
                maxLength: 253,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedEnvironment",
                table: "PasskeyCredentials");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "PasskeyCredentials");
        }
    }
}
