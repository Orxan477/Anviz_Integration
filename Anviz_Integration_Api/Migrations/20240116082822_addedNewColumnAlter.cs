using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anviz_Integration_Api.Migrations
{
    public partial class addedNewColumnAlter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WebhookUrl",
                table: "Logs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "WebhookUrl",
                table: "Logs",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
