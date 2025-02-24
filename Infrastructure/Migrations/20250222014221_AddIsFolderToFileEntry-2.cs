using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFolderToFileEntry2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SharedAccesses_Files_FileEntryId",
                table: "SharedAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_SharedAccesses_Users_UserId",
                table: "SharedAccesses");

            migrationBuilder.CreateTable(
                name: "FileVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileVersions_Files_FileEntryId",
                        column: x => x.FileEntryId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileVersions_FileEntryId",
                table: "FileVersions",
                column: "FileEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_SharedAccesses_Files_FileEntryId",
                table: "SharedAccesses",
                column: "FileEntryId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SharedAccesses_Users_UserId",
                table: "SharedAccesses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SharedAccesses_Files_FileEntryId",
                table: "SharedAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_SharedAccesses_Users_UserId",
                table: "SharedAccesses");

            migrationBuilder.DropTable(
                name: "FileVersions");

            migrationBuilder.AddForeignKey(
                name: "FK_SharedAccesses_Files_FileEntryId",
                table: "SharedAccesses",
                column: "FileEntryId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SharedAccesses_Users_UserId",
                table: "SharedAccesses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
