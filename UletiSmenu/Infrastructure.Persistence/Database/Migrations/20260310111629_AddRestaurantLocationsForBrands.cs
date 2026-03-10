using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantLocationsForBrands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RestaurantLocationId",
                table: "JobPosts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RestaurantLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StreetName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StreetNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantLocations_AspNetUsers_EmployerId",
                        column: x => x.EmployerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_RestaurantLocationId",
                table: "JobPosts",
                column: "RestaurantLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLocations_EmployerId",
                table: "RestaurantLocations",
                column: "EmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLocations_EmployerId_Name",
                table: "RestaurantLocations",
                columns: new[] { "EmployerId", "Name" });

            migrationBuilder.Sql(
                """
                INSERT INTO [RestaurantLocations] ([Id], [EmployerId], [Name], [StreetName], [StreetNumber], [City], [PostalCode], [Country], [Region])
                SELECT
                    NEWID(),
                    [u].[Id],
                    CONCAT(ISNULL([u].[Name], 'Brand'), ' - Main location'),
                    ISNULL([u].[Address_Street_Name], 'Unknown street'),
                    ISNULL([u].[Address_Street_Number], '0'),
                    ISNULL([u].[Address_City_Name], 'Unknown city'),
                    ISNULL([u].[Address_City_PostalCode_Value], '00000'),
                    ISNULL([u].[Address_City_Country_Name], 'Unknown country'),
                    ISNULL([u].[Address_City_Region_Name], 'Unknown region')
                FROM [AspNetUsers] AS [u]
                WHERE [u].[UserRole] = 'Employer'
                  AND NOT EXISTS (
                    SELECT 1
                    FROM [RestaurantLocations] AS [rl]
                    WHERE [rl].[EmployerId] = [u].[Id]
                  );
                """);

            migrationBuilder.Sql(
                """
                UPDATE [jp]
                SET [jp].[RestaurantLocationId] = [x].[Id]
                FROM [JobPosts] AS [jp]
                CROSS APPLY (
                    SELECT TOP 1 [rl].[Id]
                    FROM [RestaurantLocations] AS [rl]
                    WHERE [rl].[EmployerId] = [jp].[EmployerId]
                    ORDER BY [rl].[Name]
                ) AS [x]
                WHERE [jp].[RestaurantLocationId] IS NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_JobPosts_RestaurantLocations_RestaurantLocationId",
                table: "JobPosts",
                column: "RestaurantLocationId",
                principalTable: "RestaurantLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobPosts_RestaurantLocations_RestaurantLocationId",
                table: "JobPosts");

            migrationBuilder.DropTable(
                name: "RestaurantLocations");

            migrationBuilder.DropIndex(
                name: "IX_JobPosts_RestaurantLocationId",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "RestaurantLocationId",
                table: "JobPosts");
        }
    }
}
