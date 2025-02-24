using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFolderToFileEntry : Migration
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

            migrationBuilder.AddColumn<bool>(
                name: "IsFolder",
                table: "Files",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SharedAccesses_Files_FileEntryId",
                table: "SharedAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_SharedAccesses_Users_UserId",
                table: "SharedAccesses");

            migrationBuilder.DropColumn(
                name: "IsFolder",
                table: "Files");

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
    }
}
