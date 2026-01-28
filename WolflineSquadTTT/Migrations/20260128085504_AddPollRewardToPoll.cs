using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WolflineSquadTTT.Migrations
{
    /// <inheritdoc />
    public partial class AddPollRewardToPoll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reward_RewardAmount",
                table: "Poll");

            migrationBuilder.DropColumn(
                name: "Reward_RewardType",
                table: "Poll");
        }
    }
}
