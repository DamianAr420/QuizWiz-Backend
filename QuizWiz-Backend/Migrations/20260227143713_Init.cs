using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizWiz_Backend.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quizzes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    timelimitseconds = table.Column<int>(type: "int", nullable: false),
                    maxquestions = table.Column<int>(type: "int", nullable: false),
                    isofficial = table.Column<bool>(type: "bit", nullable: false),
                    isvisible = table.Column<bool>(type: "bit", nullable: false),
                    isplayable = table.Column<bool>(type: "bit", nullable: false),
                    isverified = table.Column<bool>(type: "bit", nullable: false),
                    issubmitted = table.Column<bool>(type: "bit", nullable: false),
                    authorid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    createdat = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quizzes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shopitems",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    price = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    imageurl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    createdat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    rarity = table.Column<int>(type: "int", nullable: false),
                    stockquantity = table.Column<int>(type: "int", nullable: true),
                    requiredlevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shopitems", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    displayname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    passwordhash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    cloudinarypublicid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    createdat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    points = table.Column<int>(type: "int", nullable: false),
                    experience = table.Column<int>(type: "int", nullable: false),
                    selectedframe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    selectedbackground = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    correctanswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    distractors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    quizid = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_questions_quizzes_quizid",
                        column: x => x.quizid,
                        principalTable: "quizzes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "userinventories",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userid = table.Column<int>(type: "int", nullable: false),
                    shopitemid = table.Column<int>(type: "int", nullable: false),
                    purchasedat = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userinventories", x => x.id);
                    table.ForeignKey(
                        name: "FK_userinventories_shopitems_shopitemid",
                        column: x => x.shopitemid,
                        principalTable: "shopitems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quizattempts",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userid = table.Column<int>(type: "int", nullable: false),
                    quizid = table.Column<int>(type: "int", nullable: false),
                    score = table.Column<int>(type: "int", nullable: false),
                    totalquestions = table.Column<int>(type: "int", nullable: false),
                    completedat = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quizattempts", x => x.id);
                    table.ForeignKey(
                        name: "FK_quizattempts_quizzes_quizid",
                        column: x => x.quizid,
                        principalTable: "quizzes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quizattempts_users_userid",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_questions_quizid",
                table: "questions",
                column: "quizid");

            migrationBuilder.CreateIndex(
                name: "IX_quizattempts_quizid",
                table: "quizattempts",
                column: "quizid");

            migrationBuilder.CreateIndex(
                name: "IX_quizattempts_userid",
                table: "quizattempts",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_userinventories_shopitemid",
                table: "userinventories",
                column: "shopitemid");

            migrationBuilder.CreateIndex(
                name: "IX_users_displayname",
                table: "users",
                column: "displayname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "quizattempts");

            migrationBuilder.DropTable(
                name: "userinventories");

            migrationBuilder.DropTable(
                name: "quizzes");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "shopitems");
        }
    }
}
