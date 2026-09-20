using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VibeStream.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackVibeMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "popularity",
                table: "tracks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "release_year",
                table: "tracks",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "popularity",
                table: "tracks");

            migrationBuilder.DropColumn(
                name: "release_year",
                table: "tracks");
        }
    }
}
