using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ridex.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverLastSeenAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "Drivers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "Drivers");
        }
    }
}
