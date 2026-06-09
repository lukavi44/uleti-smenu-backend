using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevieweeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchReviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchReviews_ApplicationId_ReviewerId",
                table: "MatchReviews",
                columns: new[] { "ApplicationId", "ReviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchReviews_RevieweeId",
                table: "MatchReviews",
                column: "RevieweeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchReviews");
        }
    }
}
