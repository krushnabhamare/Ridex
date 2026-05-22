using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ridex.Migrations
{
    /// <inheritdoc />
    public partial class AddRideDistance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DistanceInKm",
                table: "Rides",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "Rides",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistanceInKm",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "Rides");
        }
    }
}
