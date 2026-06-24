using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployerPublicSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicSlug",
                table: "AspNetUsers",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PublicSlug",
                table: "AspNetUsers",
                column: "PublicSlug",
                unique: true,
                filter: "[PublicSlug] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PublicSlug",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PublicSlug",
                table: "AspNetUsers");
        }
    }
}
