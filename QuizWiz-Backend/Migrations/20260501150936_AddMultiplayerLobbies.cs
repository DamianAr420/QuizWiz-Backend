using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuizWiz_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiplayerLobbies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lobbies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    hostid = table.Column<int>(type: "integer", nullable: false),
                    quizid = table.Column<int>(type: "integer", nullable: false),
                    maxplayers = table.Column<int>(type: "integer", nullable: false),
                    questioncount = table.Column<int>(type: "integer", nullable: false),
                    isprivate = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lobbies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lobbyplayers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lobbyid = table.Column<Guid>(type: "uuid", nullable: false),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    displayname = table.Column<string>(type: "text", nullable: false),
                    isready = table.Column<bool>(type: "boolean", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    progress = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lobbyplayers", x => x.id);
                    table.ForeignKey(
                        name: "FK_lobbyplayers_lobbies_lobbyid",
                        column: x => x.lobbyid,
                        principalTable: "lobbies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lobbyplayers_lobbyid",
                table: "lobbyplayers",
                column: "lobbyid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lobbyplayers");

            migrationBuilder.DropTable(
                name: "lobbies");
        }
    }
}
