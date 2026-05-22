using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ridex.Migrations
{
    /// <inheritdoc />
    public partial class Licenceapprove : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProfileSubmitted",
                table: "Drivers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProfileSubmitted",
                table: "Drivers");
        }
    }
}
