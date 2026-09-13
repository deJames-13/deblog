using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndCommentStatusAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "deblog",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "deblog",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "deblog",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "deblog",
                table: "Comments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "deblog",
                table: "Comments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE deblog.\"Comments\" SET \"IsDeleted\" = true, \"DeletedAt\" = NOW() WHERE \"Status\" = 2;");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsDeleted",
                schema: "deblog",
                table: "Users",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status",
                schema: "deblog",
                table: "Users",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_IsDeleted",
                schema: "deblog",
                table: "Comments",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IsDeleted",
                schema: "deblog",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Status",
                schema: "deblog",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Comments_IsDeleted",
                schema: "deblog",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "deblog",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "deblog",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "deblog",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "deblog",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "deblog",
                table: "Comments");
        }
    }
}
