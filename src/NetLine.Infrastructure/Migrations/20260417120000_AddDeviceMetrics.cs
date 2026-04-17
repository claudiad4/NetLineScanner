using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NetLine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "device_metrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceInfoId = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    ComponentName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MetricKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    NumericValue = table.Column<double>(type: "double precision", nullable: true),
                    TextValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_metrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_device_metrics_deviceinfo_DeviceInfoId",
                        column: x => x.DeviceInfoId,
                        principalTable: "deviceinfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_device_metrics_DeviceInfoId_Timestamp",
                table: "device_metrics",
                columns: new[] { "DeviceInfoId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_device_metrics_DeviceInfoId_MetricKey_Timestamp",
                table: "device_metrics",
                columns: new[] { "DeviceInfoId", "MetricKey", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_metrics");
        }
    }
}
