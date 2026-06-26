using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WolflineSquadTTT.Migrations
{
    /// <inheritdoc />
    public partial class AddPollUserInput : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WriteInText",
                table: "UserPollOptionVote",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsUserInput",
                table: "PollOption",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowUserInput",
                table: "Poll",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WriteInText",
                table: "UserPollOptionVote");

            migrationBuilder.DropColumn(
                name: "IsUserInput",
                table: "PollOption");

            migrationBuilder.DropColumn(
                name: "AllowUserInput",
                table: "Poll");
        }
    }
}
