using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddPostStatusAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "deblog",
                table: "Posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "deblog",
                table: "Posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "deblog",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE deblog.\"Posts\" SET \"Status\" = 1 WHERE \"IsPublished\" = true;");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                schema: "deblog",
                table: "Posts");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_IsDeleted",
                schema: "deblog",
                table: "Posts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Status",
                schema: "deblog",
                table: "Posts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_IsDeleted",
                schema: "deblog",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_Status",
                schema: "deblog",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "deblog",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "deblog",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "deblog",
                table: "Posts");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                schema: "deblog",
                table: "Posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
