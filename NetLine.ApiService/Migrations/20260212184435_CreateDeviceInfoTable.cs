using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NetLine.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class CreateDeviceInfoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "devicesbasicinfo");

            migrationBuilder.CreateTable(
                name: "deviceinfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserDefinedName = table.Column<string>(type: "text", nullable: false),
                    DeviceType = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PingResponseTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    SysName = table.Column<string>(type: "text", nullable: true),
                    SysDescr = table.Column<string>(type: "text", nullable: true),
                    SysLocation = table.Column<string>(type: "text", nullable: true),
                    SysContact = table.Column<string>(type: "text", nullable: true),
                    LastScanned = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deviceinfo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deviceinfo_IpAddress",
                table: "deviceinfo",
                column: "IpAddress",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deviceinfo");

            migrationBuilder.CreateTable(
                name: "devicesbasicinfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    UniqueIdOrName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devicesbasicinfo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_devicesbasicinfo_UniqueIdOrName",
                table: "devicesbasicinfo",
                column: "UniqueIdOrName",
                unique: true);
        }
    }
}
