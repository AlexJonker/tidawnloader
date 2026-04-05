using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tidawnloader.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUselessStuff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bpm",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "KeyScale",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "Manifest",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "ManifestMimeType",
                table: "Tracks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Bpm",
                table: "Tracks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Tracks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "KeyScale",
                table: "Tracks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Manifest",
                table: "Tracks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ManifestMimeType",
                table: "Tracks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
