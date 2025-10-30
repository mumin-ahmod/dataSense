using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataSenseAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectsAndProjectKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "project_id",
                table: "conversations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    project_id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    message_channel = table.Column<string>(type: "text", nullable: false),
                    channel_number = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    project_key_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.project_id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_conversations_project_id",
                table: "conversations",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "idx_projects_project_key_hash",
                table: "projects",
                column: "project_key_hash");

            migrationBuilder.CreateIndex(
                name: "idx_projects_user_active",
                table: "projects",
                columns: new[] { "user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "idx_projects_user_id",
                table: "projects",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropIndex(
                name: "idx_conversations_project_id",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "conversations");
        }
    }
}
