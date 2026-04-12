using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable

namespace NetLine.Infrastructure.Migrations
{
    public partial class AddOffice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Offices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offices", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "OfficeId",
                table: "deviceinfo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_deviceinfo_OfficeId",
                table: "deviceinfo",
                column: "OfficeId");

            migrationBuilder.AddForeignKey(
                name: "FK_deviceinfo_Offices_OfficeId",
                table: "deviceinfo",
                column: "OfficeId",
                principalTable: "Offices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deviceinfo_Offices_OfficeId",
                table: "deviceinfo");

            migrationBuilder.DropIndex(
                name: "IX_deviceinfo_OfficeId",
                table: "deviceinfo");

            migrationBuilder.DropColumn(
                name: "OfficeId",
                table: "deviceinfo");

            migrationBuilder.DropTable(
                name: "Offices");
        }
    }
}