using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizWiz_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudinaryIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvatarUrl",
                table: "Users",
                newName: "CloudinaryPublicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CloudinaryPublicId",
                table: "Users",
                newName: "AvatarUrl");
        }
    }
}
