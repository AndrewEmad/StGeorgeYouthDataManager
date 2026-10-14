/* SQL Server: execute against the YouthDataManager database.
   Preview first. Set @Apply = 1 to save recognized aliases only.
   Includes archived students; does not rewrite historical approval requests.
   Graduate labels map to حديث التخرج; غير معروف remains selectable.
   Other unresolved stages (e.g. secondary school) are shown for review.
   Optional @ActorId identifies the administrator; NULL records a system correction.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Apply bit = 0;
DECLARE @ActorId uniqueidentifier = NULL;
DECLARE @Now datetime2 = SYSUTCDATETIME();
DECLARE @Correlation nvarchar(64) = CONVERT(nvarchar(36),NEWID());
DECLARE @ActorName nvarchar(200) = COALESCE((SELECT FullName FROM dbo.AspNetUsers WHERE Id=@ActorId),N'توحيد السنوات الدراسية');
IF @ActorId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.AspNetUsers WHERE Id=@ActorId) THROW 51000,N'معرف المسؤول غير موجود.',1;

DROP TABLE IF EXISTS #YearAliases;
DROP TABLE IF EXISTS #YearPlan;
CREATE TABLE #YearAliases (AliasKey nvarchar(100) COLLATE Latin1_General_100_BIN2 PRIMARY KEY,Canonical nvarchar(100) NOT NULL);
INSERT #YearAliases(AliasKey,Canonical) VALUES
(N'اولي',N'سنة اولى'),
(N'اول',N'سنة اولى'),
(N'الاولي',N'سنة اولى'),
(N'الاول',N'سنة اولى'),
(N'1',N'سنة اولى'),
(N'١',N'سنة اولى'),
(N'۱',N'سنة اولى'),
(N'ثانيه',N'سنة ثانية'),
(N'ثاني',N'سنة ثانية'),
(N'الثانيه',N'سنة ثانية'),
(N'الثاني',N'سنة ثانية'),
(N'تانيه',N'سنة ثانية'),
(N'تاني',N'سنة ثانية'),
(N'2',N'سنة ثانية'),
(N'٢',N'سنة ثانية'),
(N'۲',N'سنة ثانية'),
(N'ثالثه',N'سنة ثالثة'),
(N'ثالث',N'سنة ثالثة'),
(N'الثالثه',N'سنة ثالثة'),
(N'الثالث',N'سنة ثالثة'),
(N'تالته',N'سنة ثالثة'),
(N'تالت',N'سنة ثالثة'),
(N'3',N'سنة ثالثة'),
(N'٣',N'سنة ثالثة'),
(N'۳',N'سنة ثالثة'),
(N'رابعه',N'سنة رابعة'),
(N'رابع',N'سنة رابعة'),
(N'الرابعه',N'سنة رابعة'),
(N'الرابع',N'سنة رابعة'),
(N'4',N'سنة رابعة'),
(N'٤',N'سنة رابعة'),
(N'۴',N'سنة رابعة'),
(N'خامسه',N'سنة خامسة'),
(N'خامس',N'سنة خامسة'),
(N'الخامسه',N'سنة خامسة'),
(N'الخامس',N'سنة خامسة'),
(N'5',N'سنة خامسة'),
(N'٥',N'سنة خامسة'),
(N'۵',N'سنة خامسة'),
(N'سادسه',N'سنة سادسة'),
(N'سادس',N'سنة سادسة'),
(N'السادسه',N'سنة سادسة'),
(N'السادس',N'سنة سادسة'),
(N'6',N'سنة سادسة'),
(N'٦',N'سنة سادسة'),
(N'۶',N'سنة سادسة'),
(N'سابعه',N'سنة سابعة'),
(N'سابع',N'سنة سابعة'),
(N'السابعه',N'سنة سابعة'),
(N'السابع',N'سنة سابعة'),
(N'7',N'سنة سابعة'),
(N'٧',N'سنة سابعة'),
(N'۷',N'سنة سابعة'),
(N'غيرمعروف',N'غير معروف'),
(N'حديثيالتخرج',N'حديث التخرج'),
(N'حديثوالتخرج',N'حديث التخرج'),
(N'حديثاتالتخرج',N'حديث التخرج'),
(N'حديثالتخرج',N'حديث التخرج'),
(N'حديثتخرج',N'حديث التخرج'),
(N'حديثهالتخرج',N'حديث التخرج'),
(N'خريجحديث',N'حديث التخرج'),
(N'خريجهحديثه',N'حديث التخرج'),
(N'خريججديد',N'حديث التخرج'),
(N'خريجهجديده',N'حديث التخرج');
SELECT Id,FullName,IsDeleted,AcademicYear AS Previous,CONVERT(binary(8),Version) AS PreviousVersion,
       CONVERT(nvarchar(max),LOWER(COALESCE(AcademicYear,N''))) COLLATE Latin1_General_100_BIN2 AS AliasKey,
       CONVERT(nvarchar(100),NULL) AS Canonical
INTO #YearPlan FROM dbo.Students;
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(32),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(9),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(13),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(10),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(160),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(8206),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(8207),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1564),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(8203),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1600),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1648),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1611),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1612),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1613),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1614),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1615),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1616),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1617),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1618),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1619),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1620),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1621),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1622),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1623),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1624),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1625),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1626),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1627),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1628),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1629),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1630),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1631),N'');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,N'أ',N'ا');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,N'إ',N'ا');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,N'آ',N'ا');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,N'ٱ',N'ا');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,N'ى',N'ي');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,N'ی',N'ي');
UPDATE #YearPlan SET AliasKey=REPLACE(AliasKey,N'ة',N'ه');
-- Strip only a parenthetical future-graduation note, including an unclosed one.
UPDATE p SET AliasKey=LEFT(p.AliasKey,n.NoteAt-1)
FROM #YearPlan p CROSS APPLY(SELECT TOP(1) CHARINDEX(v.Prefix,p.AliasKey) AS NoteAt
 FROM (VALUES(N'(هيتخرج'),(N'(سيتخرج'),(N'(هتتخرج'),(N'(ستتخرج')) v(Prefix)
 WHERE CHARINDEX(v.Prefix,p.AliasKey)>0 ORDER BY NoteAt) n;
UPDATE #YearPlan SET AliasKey=CASE
 WHEN LEFT(AliasKey,13)=N'السنهالدراسيه' THEN SUBSTRING(AliasKey,14,2147483647)
 WHEN LEFT(AliasKey,9)=N'سنهدراسيه' THEN SUBSTRING(AliasKey,10,2147483647)
 WHEN LEFT(AliasKey,12)=N'العامالدراسي' THEN SUBSTRING(AliasKey,13,2147483647)
 WHEN LEFT(AliasKey,5)=N'السنه' THEN SUBSTRING(AliasKey,6,2147483647)
 WHEN LEFT(AliasKey,3)=N'سنه' THEN SUBSTRING(AliasKey,4,2147483647)
 WHEN LEFT(AliasKey,6)=N'الفرقه' THEN SUBSTRING(AliasKey,7,2147483647)
 WHEN LEFT(AliasKey,4)=N'فرقه' THEN SUBSTRING(AliasKey,5,2147483647)
 ELSE AliasKey END;
UPDATE #YearPlan SET AliasKey=CASE
 WHEN RIGHT(AliasKey,8)=N'بالجامعه' THEN LEFT(AliasKey,LEN(AliasKey)-8)
 WHEN RIGHT(AliasKey,6)=N'جامعيه' THEN LEFT(AliasKey,LEN(AliasKey)-6)
 WHEN RIGHT(AliasKey,5)=N'جامعي' THEN LEFT(AliasKey,LEN(AliasKey)-5)
 WHEN RIGHT(AliasKey,5)=N'جامعه' THEN LEFT(AliasKey,LEN(AliasKey)-5)
 ELSE AliasKey END;
UPDATE p SET Canonical=m.Canonical FROM #YearPlan p JOIN #YearAliases m ON m.AliasKey=p.AliasKey;
UPDATE #YearPlan SET Canonical=N'حديث التخرج' WHERE Canonical IS NULL AND
 (AliasKey LIKE N'خريج%' OR AliasKey LIKE N'الخريج%' OR AliasKey LIKE N'اتخرجت%'
  OR AliasKey LIKE N'تخرجت%' OR AliasKey LIKE N'متخرج%' OR AliasKey LIKE N'المتخرج%');
-- Preview changed rows, then unknown nonempty values. Empty values are not assigned a year.
SELECT Id,FullName,IsDeleted,Previous AS [السنة الحالية],Canonical AS [السنة الموحدة]
FROM #YearPlan WHERE Canonical IS NOT NULL AND CONVERT(varbinary(max),COALESCE(Previous,N''))<>CONVERT(varbinary(max),Canonical) ORDER BY FullName,Id;
SELECT Previous AS [قيمة تحتاج مراجعة],COUNT(*) AS [عدد المخدومين] FROM #YearPlan WHERE Canonical IS NULL AND AliasKey<>N'' GROUP BY Previous ORDER BY COUNT(*) DESC,Previous;
IF @Apply=0 RETURN;

BEGIN TRY
 BEGIN TRANSACTION;
 -- Fail rather than overwrite a record edited since the preview snapshot.
 IF EXISTS(SELECT 1 FROM #YearPlan p JOIN dbo.Students s WITH(UPDLOCK,HOLDLOCK) ON s.Id=p.Id
           WHERE p.Canonical IS NOT NULL AND CONVERT(varbinary(max),COALESCE(p.Previous,N''))<>CONVERT(varbinary(max),p.Canonical) AND s.Version<>p.PreviousVersion)
     THROW 51001,N'تغيرت بيانات بعض المخدومين. أعد تشغيل المعاينة قبل الحفظ.',1;
 DECLARE @Changed TABLE(Id uniqueidentifier,Previous nvarchar(max),Canonical nvarchar(100));
 UPDATE s SET AcademicYear=p.Canonical,UpdatedAt=@Now
 OUTPUT inserted.Id,deleted.AcademicYear,inserted.AcademicYear INTO @Changed
 FROM dbo.Students s JOIN #YearPlan p ON p.Id=s.Id
 WHERE p.Canonical IS NOT NULL AND CONVERT(varbinary(max),COALESCE(p.Previous,N''))<>CONVERT(varbinary(max),p.Canonical);
 INSERT dbo.StudentEditLogs(Id,StudentId,UpdatedAt,UpdatedByUserId,UpdatedByUserName,Details)
 SELECT NEWID(),Id,@Now,COALESCE(@ActorId,'00000000-0000-0000-0000-000000000000'),@ActorName,
        CONCAT(N'توحيد السنة الدراسية من «',Previous,N'» إلى «',Canonical,N'».') FROM @Changed;
 INSERT dbo.AuditEntry(Id,ActorId,Timestamp,EntityType,EntityId,Action,BeforeJson,AfterJson,CorrelationId,Reason)
 SELECT NEWID(),@ActorId,@Now,N'Student',CONVERT(nvarchar(64),c.Id),N'Modified',
        (SELECT c.Previous AS AcademicYear FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
        (SELECT c.Canonical AS AcademicYear FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
        @Correlation,N'توحيد السنوات الدراسية' FROM @Changed c;
 COMMIT;
 SELECT COUNT(*) AS [عدد المخدومين الذين تم توحيد سنتهم] FROM @Changed;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
