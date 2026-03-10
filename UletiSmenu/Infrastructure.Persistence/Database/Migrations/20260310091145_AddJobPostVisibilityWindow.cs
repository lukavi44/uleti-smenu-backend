using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPostVisibilityWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "VisibleUntil",
                table: "JobPosts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [JobPosts]
                SET [VisibleUntil] = DATEADD(hour, 1, [StartingDate])
                WHERE [VisibleUntil] IS NULL
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "VisibleUntil",
                table: "JobPosts",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_VisibleUntil",
                table: "JobPosts",
                column: "VisibleUntil");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobPosts_VisibleUntil",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "VisibleUntil",
                table: "JobPosts");
        }
    }
}
