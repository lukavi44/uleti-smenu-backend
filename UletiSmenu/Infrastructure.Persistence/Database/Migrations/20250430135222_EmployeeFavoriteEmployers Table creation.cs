using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeFavoriteEmployersTablecreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CvFileName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            //    migrationBuilder.CreateTable(
            //        name: "EmployeeFavoriteEmployers",
            //        columns: table => new
            //        {
            //            EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            //            EmployerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_EmployeeFavoriteEmployers", x => new { x.EmployeeId, x.EmployerId });
            //            table.ForeignKey(
            //                name: "FK_EmployeeFavoriteEmployers_AspNetUsers_EmployeeId",
            //                column: x => x.EmployeeId,
            //                principalTable: "AspNetUsers",
            //                principalColumn: "Id");
            //            table.ForeignKey(
            //                name: "FK_EmployeeFavoriteEmployers_AspNetUsers_EmployerId",
            //                column: x => x.EmployerId,
            //                principalTable: "AspNetUsers",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateIndex(
            //        name: "IX_EmployeeFavoriteEmployers_EmployerId",
            //        table: "EmployeeFavoriteEmployers",
            //        column: "EmployerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeFavoriteEmployers");

            migrationBuilder.DropColumn(
                name: "CvFileName",
                table: "AspNetUsers");
        }
    }
}
