using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetLine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerTierScanTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastLightScanAt",
                table: "deviceinfo",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMediumScanAt",
                table: "deviceinfo",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastHeavyScanAt",
                table: "deviceinfo",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLightScanAt",
                table: "deviceinfo");

            migrationBuilder.DropColumn(
                name: "LastMediumScanAt",
                table: "deviceinfo");

            migrationBuilder.DropColumn(
                name: "LastHeavyScanAt",
                table: "deviceinfo");
        }
    }
}
