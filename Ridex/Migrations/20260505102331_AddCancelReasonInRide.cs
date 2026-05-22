using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ridex.Migrations
{
    /// <inheritdoc />
    public partial class AddCancelReasonInRide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "Rides",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "Rides");
        }
    }
}
