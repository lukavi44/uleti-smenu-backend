using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDeletedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DeletedAtUtc",
                table: "AspNetUsers",
                column: "DeletedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DeletedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "AspNetUsers");
        }
    }
}
