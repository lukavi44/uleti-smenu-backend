-- LIVE database verification (run in Azure Data Studio against UletiSmenuDb_Staging / LIVE DB)
-- Expected after Phase 3 migration on a clean database.

SET NOCOUNT ON;

PRINT '=== Migration history ===';
SELECT MigrationId
FROM __EFMigrationsHistory
ORDER BY MigrationId;

PRINT '';
PRINT '=== Phase 3 checks ===';

IF COL_LENGTH('Applications', 'NumberOfApplicants') IS NOT NULL
    PRINT 'FAIL: NumberOfApplicants column still exists';
ELSE
    PRINT 'OK: NumberOfApplicants removed';

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Applications_UserId_JobPostId'
      AND object_id = OBJECT_ID('Applications')
      AND is_unique = 1)
    PRINT 'OK: unique IX_Applications_UserId_JobPostId exists';
ELSE
    PRINT 'FAIL: unique IX_Applications_UserId_JobPostId missing';

PRINT '';
PRINT '=== Duplicate application pairs (must be 0) ===';
SELECT UserId, JobPostId, COUNT(*) AS PairCount
FROM Applications
GROUP BY UserId, JobPostId
HAVING COUNT(*) > 1;

IF @@ROWCOUNT = 0
    PRINT 'OK: no duplicate (UserId, JobPostId) pairs';

PRINT '';
PRINT '=== Account deletion column ===';
IF COL_LENGTH('AspNetUsers', 'DeletedAtUtc') IS NULL
    PRINT 'WARN: AspNetUsers.DeletedAtUtc missing (expected after AddUserDeletedAtUtc migration)'
ELSE
    PRINT 'OK: AspNetUsers.DeletedAtUtc present';

PRINT '';
PRINT '=== Row counts ===';
SELECT 'Applications' AS [Table], COUNT(*) AS Cnt FROM Applications
UNION ALL SELECT 'Conversations', COUNT(*) FROM Conversations
UNION ALL SELECT 'Notifications', COUNT(*) FROM Notifications;

IF COL_LENGTH('AspNetUsers', 'DeletedAtUtc') IS NOT NULL
BEGIN
    SELECT 'AspNetUsers (DeletedAtUtc set)' AS [Table], COUNT(*) AS Cnt
    FROM AspNetUsers
    WHERE DeletedAtUtc IS NOT NULL;
END
ELSE
    PRINT 'SKIP: tombstone user count (DeletedAtUtc not migrated on LIVE yet — expected until account-deletion deploy)';

PRINT '';
PRINT '=== Constraint check ===';
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
