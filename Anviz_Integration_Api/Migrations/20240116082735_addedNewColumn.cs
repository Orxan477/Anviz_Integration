using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anviz_Integration_Api.Migrations
{
    public partial class addedNewColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WebhookUrl",
                table: "Logs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WebhookUrl",
                table: "Logs");
        }
    }
}
