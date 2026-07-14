using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class EnforceGeographyHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_GeographyCities_GeographyCityCode",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_GeographyRegions_GeographyRegionCode",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantLocations_GeographyCities_GeographyCityCode",
                table: "RestaurantLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantLocations_GeographyRegions_GeographyRegionCode",
                table: "RestaurantLocations");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_GeographyRegions_CountryCode_Code",
                table: "GeographyRegions",
                columns: new[] { "CountryCode", "Code" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_GeographyCities_RegionCode_Code",
                table: "GeographyCities",
                columns: new[] { "RegionCode", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLocations_GeographyCountryCode_GeographyRegionCode",
                table: "RestaurantLocations",
                columns: new[] { "GeographyCountryCode", "GeographyRegionCode" });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLocations_GeographyRegionCode_GeographyCityCode",
                table: "RestaurantLocations",
                columns: new[] { "GeographyRegionCode", "GeographyCityCode" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GeographyCountryCode_GeographyRegionCode",
                table: "AspNetUsers",
                columns: new[] { "GeographyCountryCode", "GeographyRegionCode" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GeographyRegionCode_GeographyCityCode",
                table: "AspNetUsers",
                columns: new[] { "GeographyRegionCode", "GeographyCityCode" });

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_GeographyCities_GeographyRegionCode_GeographyCityCode",
                table: "AspNetUsers",
                columns: new[] { "GeographyRegionCode", "GeographyCityCode" },
                principalTable: "GeographyCities",
                principalColumns: new[] { "RegionCode", "Code" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_GeographyRegions_GeographyCountryCode_GeographyRegionCode",
                table: "AspNetUsers",
                columns: new[] { "GeographyCountryCode", "GeographyRegionCode" },
                principalTable: "GeographyRegions",
                principalColumns: new[] { "CountryCode", "Code" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantLocations_GeographyCities_GeographyRegionCode_GeographyCityCode",
                table: "RestaurantLocations",
                columns: new[] { "GeographyRegionCode", "GeographyCityCode" },
                principalTable: "GeographyCities",
                principalColumns: new[] { "RegionCode", "Code" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantLocations_GeographyRegions_GeographyCountryCode_GeographyRegionCode",
                table: "RestaurantLocations",
                columns: new[] { "GeographyCountryCode", "GeographyRegionCode" },
                principalTable: "GeographyRegions",
                principalColumns: new[] { "CountryCode", "Code" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_GeographyCities_GeographyRegionCode_GeographyCityCode",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_GeographyRegions_GeographyCountryCode_GeographyRegionCode",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantLocations_GeographyCities_GeographyRegionCode_GeographyCityCode",
                table: "RestaurantLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantLocations_GeographyRegions_GeographyCountryCode_GeographyRegionCode",
                table: "RestaurantLocations");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantLocations_GeographyCountryCode_GeographyRegionCode",
                table: "RestaurantLocations");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantLocations_GeographyRegionCode_GeographyCityCode",
                table: "RestaurantLocations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_GeographyRegions_CountryCode_Code",
                table: "GeographyRegions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_GeographyCities_RegionCode_Code",
                table: "GeographyCities");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_GeographyCountryCode_GeographyRegionCode",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_GeographyRegionCode_GeographyCityCode",
                table: "AspNetUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_GeographyCities_GeographyCityCode",
                table: "AspNetUsers",
                column: "GeographyCityCode",
                principalTable: "GeographyCities",
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
                name: "FK_RestaurantLocations_GeographyRegions_GeographyRegionCode",
                table: "RestaurantLocations",
                column: "GeographyRegionCode",
                principalTable: "GeographyRegions",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
