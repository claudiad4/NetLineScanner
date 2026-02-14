using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetLine.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InterfacesCount",
                table: "deviceinfo",
                newName: "SysInterfacesCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SysInterfacesCount",
                table: "deviceinfo",
                newName: "InterfacesCount");
        }
    }
}
