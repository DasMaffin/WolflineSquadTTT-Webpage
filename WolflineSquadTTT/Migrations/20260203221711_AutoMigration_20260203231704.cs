using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WolflineSquadTTT.Migrations
{
    /// <inheritdoc />
    public partial class AutoMigration_20260203231704 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPollOptionVote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PollOptionFK = table.Column<int>(type: "int", nullable: false),
                    UserFK = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPollOptionVote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPollOptionVote_PollOption_PollOptionFK",
                        column: x => x.PollOptionFK,
                        principalTable: "PollOption",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPollOptionVote_User_UserFK",
                        column: x => x.UserFK,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserPollOptionVote_PollOptionFK",
                table: "UserPollOptionVote",
                column: "PollOptionFK");

            migrationBuilder.CreateIndex(
                name: "IX_UserPollOptionVote_UserFK",
                table: "UserPollOptionVote",
                column: "UserFK");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPollOptionVote");
        }
    }
}
