using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetLine.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreSnmpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InterfacesCount",
                table: "deviceinfo",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SysUpTime",
                table: "deviceinfo",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterfacesCount",
                table: "deviceinfo");

            migrationBuilder.DropColumn(
                name: "SysUpTime",
                table: "deviceinfo");
        }
    }
}
