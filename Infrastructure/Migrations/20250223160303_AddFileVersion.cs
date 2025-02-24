using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFileVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Path",
                table: "FileVersions",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "ContentType",
                table: "FileVersions",
                newName: "FilePath");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "FileVersions",
                newName: "FileVersionId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SharedAccesses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SharedAccesses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "FileVersions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Files",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Files",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SharedAccesses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SharedAccesses");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "FileVersions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Files");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "FileVersions",
                newName: "Path");

            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "FileVersions",
                newName: "ContentType");

            migrationBuilder.RenameColumn(
                name: "FileVersionId",
                table: "FileVersions",
                newName: "Id");
        }
    }
}
