using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class ExpandFeaturesAndSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                schema: "deblog",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Guest");

            migrationBuilder.AddColumn<bool>(
                name: "IsGuest",
                schema: "deblog",
                table: "Comments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ManagementToken",
                schema: "deblog",
                table: "Comments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "deblog",
                table: "Comments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PostAnalytics",
                schema: "deblog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Views = table.Column<int>(type: "integer", nullable: false),
                    Likes = table.Column<int>(type: "integer", nullable: false),
                    Shares = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostAnalytics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostAnalytics_Posts_PostId",
                        column: x => x.PostId,
                        principalSchema: "deblog",
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ManagementToken",
                schema: "deblog",
                table: "Comments",
                column: "ManagementToken");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_Status",
                schema: "deblog",
                table: "Comments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PostAnalytics_PostId",
                schema: "deblog",
                table: "PostAnalytics",
                column: "PostId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostAnalytics",
                schema: "deblog");

            migrationBuilder.DropIndex(
                name: "IX_Comments_ManagementToken",
                schema: "deblog",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_Status",
                schema: "deblog",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Role",
                schema: "deblog",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsGuest",
                schema: "deblog",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ManagementToken",
                schema: "deblog",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "deblog",
                table: "Comments");
        }
    }
}
