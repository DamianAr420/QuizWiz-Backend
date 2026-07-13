using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizWiz_Backend.Migrations
{
    /// <inheritdoc />
    public partial class LobbyUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "selectedbackground",
                table: "lobbyplayers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "selectedframe",
                table: "lobbyplayers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "selectedbackground",
                table: "lobbyplayers");

            migrationBuilder.DropColumn(
                name: "selectedframe",
                table: "lobbyplayers");
        }
    }
}
