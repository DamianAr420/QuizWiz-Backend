using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizWiz_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizIsVisibleIsPlayableAuthorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorId",
                table: "Quizzes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPlayable",
                table: "Quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "Quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "IsPlayable",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "Quizzes");
        }
    }
}
