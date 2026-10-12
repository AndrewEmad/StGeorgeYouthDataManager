-- Read-only review before ReviewedLegacyConstraints. Execute against the selected staging clone first.
SET NOCOUNT ON;
SELECT 'Secondary phone exceeds 20 characters' AS Finding, Id AS StudentId, DATALENGTH(SecondaryPhone)/2 AS Characters FROM dbo.Students WHERE DATALENGTH(SecondaryPhone)>40;
SELECT 'Duplicate pending assignment' AS Finding,StudentId,RequestedByUserId,COUNT(*) AS PendingCount FROM dbo.StudentAssignmentRequests WHERE Status='Pending' GROUP BY StudentId,RequestedByUserId HAVING COUNT(*)>1;
SELECT 'Duplicate pending removal' AS Finding,StudentId,RequestedByUserId,COUNT(*) AS PendingCount FROM dbo.StudentRemovalRequests WHERE Status='Pending' GROUP BY StudentId,RequestedByUserId HAVING COUNT(*)>1;
SELECT 'Conflicting full date and birthday month' AS Finding,Id AS StudentId,BirthDate,BirthMonth FROM dbo.Students WHERE BirthDate IS NOT NULL AND (BirthMonth IS NULL OR BirthMonth<>MONTH(BirthDate));
SELECT 'Invalid month-only birthday' AS Finding,Id AS StudentId,BirthMonth FROM dbo.Students WHERE BirthDate IS NULL AND BirthMonth IS NOT NULL AND BirthMonth NOT BETWEEN 1 AND 12;
SELECT 'Original legacy migration history' AS Finding,MigrationId,ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
