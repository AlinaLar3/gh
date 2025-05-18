using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileStoringService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FilesMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", nullable: false),
                    StorageLocation = table.Column<string>(type: "TEXT", nullable: false),
                    UploadTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilesMetadata", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FilesMetadata_FileHash",
                table: "FilesMetadata",
                column: "FileHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FilesMetadata");
        }
    }
}
