using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MQTTRestApi.Migrations
{
    /// <inheritdoc />
    public partial class AlarmsNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AlarmType",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlarmType",
                table: "Messages");
        }
    }
}
