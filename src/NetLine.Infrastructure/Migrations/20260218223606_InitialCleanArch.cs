using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

//note from Claudia: when I tried to refactor the code, I messed up the migration history, so I had to delete all migrations and create a new one with the current state of the code.

#nullable disable

namespace NetLine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCleanArch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    SysUpTime = table.Column<string>(type: "text", nullable: true),
                    SysInterfacesCount = table.Column<int>(type: "integer", nullable: true),
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
        }
    }
}
