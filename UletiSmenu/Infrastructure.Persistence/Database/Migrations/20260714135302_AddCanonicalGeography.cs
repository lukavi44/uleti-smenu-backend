using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCanonicalGeography : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PhoneNumber",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "GeographyCityCode",
                table: "RestaurantLocations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeographyCountryCode",
                table: "RestaurantLocations",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeographyRegionCode",
                table: "RestaurantLocations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeographyCityCode",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeographyCountryCode",
                table: "AspNetUsers",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeographyRegionCode",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GeographyCountries",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NativeName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeographyCountries", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "GeographyRegions",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    NativeName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeographyRegions", x => x.Code);
                    table.ForeignKey(
                        name: "FK_GeographyRegions_GeographyCountries_CountryCode",
                        column: x => x.CountryCode,
                        principalTable: "GeographyCountries",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GeographyCities",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RegionCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    NativeName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeographyCities", x => x.Code);
                    table.ForeignKey(
                        name: "FK_GeographyCities_GeographyRegions_RegionCode",
                        column: x => x.RegionCode,
                        principalTable: "GeographyRegions",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLocations_GeographyCityCode",
                table: "RestaurantLocations",
                column: "GeographyCityCode");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLocations_GeographyCountryCode",
                table: "RestaurantLocations",
                column: "GeographyCountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLocations_GeographyRegionCode",
                table: "RestaurantLocations",
                column: "GeographyRegionCode");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GeographyCityCode",
                table: "AspNetUsers",
                column: "GeographyCityCode");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GeographyCountryCode",
                table: "AspNetUsers",
                column: "GeographyCountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GeographyRegionCode",
                table: "AspNetUsers",
                column: "GeographyRegionCode");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PhoneNumber",
                table: "AspNetUsers",
                column: "PhoneNumber",
                unique: true,
                filter: "[PhoneNumber] IS NOT NULL AND [PhoneNumber] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_GeographyCities_RegionCode_Name",
                table: "GeographyCities",
                columns: new[] { "RegionCode", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_GeographyCountries_Name",
                table: "GeographyCountries",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_GeographyRegions_CountryCode_Name",
                table: "GeographyRegions",
                columns: new[] { "CountryCode", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_GeographyCities_GeographyCityCode",
                table: "AspNetUsers",
                column: "GeographyCityCode",
                principalTable: "GeographyCities",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_GeographyCountries_GeographyCountryCode",
                table: "AspNetUsers",
                column: "GeographyCountryCode",
                principalTable: "GeographyCountries",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_GeographyRegions_GeographyRegionCode",
                table: "AspNetUsers",
                column: "GeographyRegionCode",
                principalTable: "GeographyRegions",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantLocations_GeographyCities_GeographyCityCode",
                table: "RestaurantLocations",
                column: "GeographyCityCode",
                principalTable: "GeographyCities",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantLocations_GeographyCountries_GeographyCountryCode",
                table: "RestaurantLocations",
                column: "GeographyCountryCode",
                principalTable: "GeographyCountries",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantLocations_GeographyRegions_GeographyRegionCode",
                table: "RestaurantLocations",
                column: "GeographyRegionCode",
                principalTable: "GeographyRegions",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_GeographyCities_GeographyCityCode",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_GeographyCountries_GeographyCountryCode",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_GeographyRegions_GeographyRegionCode",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantLocations_GeographyCities_GeographyCityCode",
                table: "RestaurantLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantLocations_GeographyCountries_GeographyCountryCode",
                table: "RestaurantLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantLocations_GeographyRegions_GeographyRegionCode",
                table: "RestaurantLocations");

            migrationBuilder.DropTable(
                name: "GeographyCities");

            migrationBuilder.DropTable(
                name: "GeographyRegions");

            migrationBuilder.DropTable(
                name: "GeographyCountries");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantLocations_GeographyCityCode",
                table: "RestaurantLocations");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantLocations_GeographyCountryCode",
                table: "RestaurantLocations");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantLocations_GeographyRegionCode",
                table: "RestaurantLocations");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_GeographyCityCode",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_GeographyCountryCode",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_GeographyRegionCode",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PhoneNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GeographyCityCode",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "GeographyCountryCode",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "GeographyRegionCode",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "GeographyCityCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GeographyCountryCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GeographyRegionCode",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PhoneNumber",
                table: "AspNetUsers",
                column: "PhoneNumber",
                unique: true,
                filter: "[PhoneNumber] IS NOT NULL");
        }
    }
}
