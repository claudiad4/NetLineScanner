using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetLine.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class DodanieKolumnyIp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "devicesbasicinfo",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "devicesbasicinfo");
        }
    }
}
