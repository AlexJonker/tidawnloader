using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tidawnloader.Migrations
{
    /// <inheritdoc />
    public partial class AddDownloadEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDownloaded",
                table: "Tracks",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShouldDownload",
                table: "Tracks",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDownloaded",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "ShouldDownload",
                table: "Tracks");
        }
    }
}
