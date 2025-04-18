using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeOnJobPostEmployer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobPosts_Companies_CompanyId",
                table: "JobPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_JobPosts_Companies_CompanyId1",
                table: "JobPosts");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_JobPosts_CompanyId1",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "CompanyId1",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "JobPosts",
                newName: "EmployerId");

            migrationBuilder.RenameIndex(
                name: "IX_JobPosts_CompanyId",
                table: "JobPosts",
                newName: "IX_JobPosts_EmployerId");

            migrationBuilder.AlterColumn<string>(
                name: "ProfilePhoto",
                table: "AspNetUsers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "Address_City_Country_Name",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_City_Name",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_City_PostalCode_Value",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_City_Region_Name",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_Street_Name",
                table: "AspNetUsers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_Street_Number",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_JobPosts_AspNetUsers_EmployerId",
                table: "JobPosts",
                column: "EmployerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobPosts_AspNetUsers_EmployerId",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "Address_City_Country_Name",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Address_City_Name",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Address_City_PostalCode_Value",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Address_City_Region_Name",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Address_Street_Name",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Address_Street_Number",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "EmployerId",
                table: "JobPosts",
                newName: "CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_JobPosts_EmployerId",
                table: "JobPosts",
                newName: "IX_JobPosts_CompanyId");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId1",
                table: "JobPosts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProfilePhoto",
                table: "AspNetUsers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Address_City_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address_City_Country_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address_City_PostalCode_Value = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address_City_Region_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address_Street_Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Address_Street_Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_CompanyId1",
                table: "JobPosts",
                column: "CompanyId1");

            migrationBuilder.AddForeignKey(
                name: "FK_JobPosts_Companies_CompanyId",
                table: "JobPosts",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobPosts_Companies_CompanyId1",
                table: "JobPosts",
                column: "CompanyId1",
                principalTable: "Companies",
                principalColumn: "Id");
        }
    }
}
