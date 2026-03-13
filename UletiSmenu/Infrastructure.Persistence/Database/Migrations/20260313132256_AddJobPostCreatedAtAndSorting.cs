using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPostCreatedAtAndSorting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "JobPosts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [JobPosts]
                SET [CreatedAtUtc] = [StartingDate]
                WHERE [CreatedAtUtc] IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "JobPosts",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_CreatedAtUtc",
                table: "JobPosts",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobPosts_CreatedAtUtc",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "JobPosts");
        }
    }
}
