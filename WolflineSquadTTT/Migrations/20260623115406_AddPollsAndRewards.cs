using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WolflineSquadTTT.Migrations
{
    /// <inheritdoc />
    public partial class AddPollsAndRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PollType",
                table: "Poll");

            migrationBuilder.DropColumn(
                name: "Reward_RewardAmount",
                table: "Poll");

            migrationBuilder.DropColumn(
                name: "Reward_RewardType",
                table: "Poll");

            migrationBuilder.RenameColumn(
                name: "Rank",
                table: "UserPollOptionVote",
                newName: "Placement");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserPollOptionVote",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "PollOption",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Poll",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Poll",
                type: "varchar(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "MaxSelections",
                table: "Poll",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RewardFK",
                table: "Poll",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Reward",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RewardType = table.Column<int>(type: "int", nullable: false),
                    NormalPoints = table.Column<int>(type: "int", nullable: false),
                    PremiumPoints = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reward", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RewardClaim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserFK = table.Column<int>(type: "int", nullable: false),
                    RewardFK = table.Column<int>(type: "int", nullable: false),
                    PollFK = table.Column<int>(type: "int", nullable: false),
                    Claimed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RewardClaim_Poll_PollFK",
                        column: x => x.PollFK,
                        principalTable: "Poll",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RewardClaim_Reward_RewardFK",
                        column: x => x.RewardFK,
                        principalTable: "Reward",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RewardClaim_User_UserFK",
                        column: x => x.UserFK,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserPollOptionVote_UserFK_PollOptionFK",
                table: "UserPollOptionVote",
                columns: new[] { "UserFK", "PollOptionFK" },
                unique: true);

            // Drop the old single-column index only after the composite index (UserFK leftmost)
            // exists, so the UserFK foreign key always keeps a backing index (MySQL requirement).
            migrationBuilder.DropIndex(
                name: "IX_UserPollOptionVote_UserFK",
                table: "UserPollOptionVote");

            migrationBuilder.CreateIndex(
                name: "IX_Poll_RewardFK",
                table: "Poll",
                column: "RewardFK");

            migrationBuilder.CreateIndex(
                name: "IX_RewardClaim_PollFK",
                table: "RewardClaim",
                column: "PollFK");

            migrationBuilder.CreateIndex(
                name: "IX_RewardClaim_RewardFK",
                table: "RewardClaim",
                column: "RewardFK");

            migrationBuilder.CreateIndex(
                name: "IX_RewardClaim_UserFK_PollFK",
                table: "RewardClaim",
                columns: new[] { "UserFK", "PollFK" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Poll_Reward_RewardFK",
                table: "Poll",
                column: "RewardFK",
                principalTable: "Reward",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Poll_Reward_RewardFK",
                table: "Poll");

            migrationBuilder.DropTable(
                name: "RewardClaim");

            migrationBuilder.DropTable(
                name: "Reward");

            migrationBuilder.DropIndex(
                name: "IX_UserPollOptionVote_UserFK_PollOptionFK",
                table: "UserPollOptionVote");

            migrationBuilder.DropIndex(
                name: "IX_Poll_RewardFK",
                table: "Poll");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserPollOptionVote");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "PollOption");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Poll");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Poll");

            migrationBuilder.DropColumn(
                name: "MaxSelections",
                table: "Poll");

            migrationBuilder.DropColumn(
                name: "RewardFK",
                table: "Poll");

            migrationBuilder.RenameColumn(
                name: "Placement",
                table: "UserPollOptionVote",
                newName: "Rank");

            migrationBuilder.AddColumn<int>(
                name: "PollType",
                table: "Poll",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Reward_RewardAmount",
                table: "Poll",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Reward_RewardType",
                table: "Poll",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserPollOptionVote_UserFK",
                table: "UserPollOptionVote",
                column: "UserFK");
        }
    }
}
