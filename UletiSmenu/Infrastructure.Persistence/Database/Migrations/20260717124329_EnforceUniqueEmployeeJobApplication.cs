using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueEmployeeJobApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID('tempdb..#DuplicateApplications') IS NOT NULL
                    DROP TABLE #DuplicateApplications;

                ;WITH RankedApplications AS
                (
                    SELECT
                        [Id],
                        FIRST_VALUE([Id]) OVER
                        (
                            PARTITION BY [UserId], [JobPostId]
                            ORDER BY [DateTime], [Id]
                        ) AS [KeeperId],
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY [UserId], [JobPostId]
                            ORDER BY [DateTime], [Id]
                        ) AS [DuplicateRank]
                    FROM [Applications]
                )
                SELECT [Id] AS [DuplicateId], [KeeperId]
                INTO #DuplicateApplications
                FROM RankedApplications
                WHERE [DuplicateRank] > 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM #DuplicateApplications duplicateApplication
                    INNER JOIN [Conversations] conversation
                        ON conversation.[ApplicationId] = duplicateApplication.[DuplicateId]
                )
                OR EXISTS
                (
                    SELECT 1
                    FROM #DuplicateApplications duplicateApplication
                    INNER JOIN [MatchReviews] review
                        ON review.[ApplicationId] = duplicateApplication.[DuplicateId]
                )
                BEGIN
                    THROW 51000,
                        'Duplicate applications with conversations or reviews require manual reconciliation before this migration can continue.',
                        1;
                END;

                DELETE notification
                FROM [Notifications] notification
                INNER JOIN #DuplicateApplications duplicateApplication
                    ON notification.[Type] =
                        'ApplicationReceived:' + CONVERT(nvarchar(36), duplicateApplication.[DuplicateId]);

                DELETE application
                FROM [Applications] application
                INNER JOIN #DuplicateApplications duplicateApplication
                    ON application.[Id] = duplicateApplication.[DuplicateId];

                DROP TABLE #DuplicateApplications;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Applications_UserId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "NumberOfApplicants",
                table: "Applications");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_UserId_JobPostId",
                table: "Applications",
                columns: new[] { "UserId", "JobPostId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applications_UserId_JobPostId",
                table: "Applications");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfApplicants",
                table: "Applications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_UserId",
                table: "Applications",
                column: "UserId");
        }
    }
}
