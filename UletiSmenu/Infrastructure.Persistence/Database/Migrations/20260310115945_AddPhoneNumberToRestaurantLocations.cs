using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneNumberToRestaurantLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "RestaurantLocations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [rl]
                SET [rl].[PhoneNumber] = ISNULL([u].[PhoneNumber], 'N/A')
                FROM [RestaurantLocations] AS [rl]
                INNER JOIN [AspNetUsers] AS [u] ON [u].[Id] = [rl].[EmployerId]
                WHERE [rl].[PhoneNumber] IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "RestaurantLocations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "RestaurantLocations");
        }
    }
}
