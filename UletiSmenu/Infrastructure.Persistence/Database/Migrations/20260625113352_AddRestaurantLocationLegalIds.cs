using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantLocationLegalIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MB",
                table: "RestaurantLocations",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PIB",
                table: "RestaurantLocations",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [rl]
                SET [rl].[PIB] = [u].[PIB],
                    [rl].[MB] = [u].[MB]
                FROM [RestaurantLocations] AS [rl]
                INNER JOIN [AspNetUsers] AS [u] ON [u].[Id] = [rl].[EmployerId]
                WHERE [rl].[PIB] IS NULL OR [rl].[MB] IS NULL OR [rl].[PIB] = '' OR [rl].[MB] = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "MB",
                table: "RestaurantLocations",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(8)",
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PIB",
                table: "RestaurantLocations",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(9)",
                oldMaxLength: 9,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MB",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "PIB",
                table: "RestaurantLocations");
        }
    }
}
