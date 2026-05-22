using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ridex.Migrations
{
    /// <inheritdoc />
    public partial class AddEmergencyAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmergencyAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RideId = table.Column<int>(type: "int", nullable: false),
                    RiderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RiderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiderPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyAlerts_AspNetUsers_RiderId",
                        column: x => x.RiderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmergencyAlerts_Rides_RideId",
                        column: x => x.RideId,
                        principalTable: "Rides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyAlerts_RideId",
                table: "EmergencyAlerts",
                column: "RideId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyAlerts_RiderId",
                table: "EmergencyAlerts",
                column: "RiderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmergencyAlerts");
        }
    }
}
