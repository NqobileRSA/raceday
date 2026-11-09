using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Api.Data.Migrations
{
    /// <summary>
    /// Part 3: adds the profile picture URL (Azure Blob Storage) to Users and a headline distance to Events.
    /// Both columns are nullable, so existing rows and the Part 1 seed data are unaffected.
    /// </summary>
    public partial class AddProfilePictureAndEventDistance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "profile_picture_url",
                table: "Users",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "distance_km",
                table: "Events",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "profile_picture_url", table: "Users");
            migrationBuilder.DropColumn(name: "distance_km", table: "Events");
        }
    }
}
