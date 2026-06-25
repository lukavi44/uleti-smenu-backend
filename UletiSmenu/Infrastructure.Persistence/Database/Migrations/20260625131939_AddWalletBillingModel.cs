using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletBillingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WalletBalance",
                table: "AspNetUsers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExternalReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_EmployerId",
                table: "WalletTransactions",
                column: "EmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_ExternalReference",
                table: "WalletTransactions",
                column: "ExternalReference",
                unique: true,
                filter: "[ExternalReference] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "WalletBalance",
                table: "AspNetUsers");
        }
    }
}
