using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyEmailFavouriteJobPost",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyInAppApplicationAccepted",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyInAppApplicationDeclined",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyInAppApplicationReceived",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyInAppFavouriteJobPost",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyInAppReviewReminder",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyEmailFavouriteJobPost",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NotifyInAppApplicationAccepted",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NotifyInAppApplicationDeclined",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NotifyInAppApplicationReceived",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NotifyInAppFavouriteJobPost",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NotifyInAppReviewReminder",
                table: "AspNetUsers");
        }
    }
}
