using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizWiz_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Quizzes");
        }
    }
}
