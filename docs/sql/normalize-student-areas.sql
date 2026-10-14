/* SQL Server: run against the YouthDataManager database.
   مهم: @Apply=0 يعرض معاينة فقط ولا يحفظ أي تغيير.
   للحفظ غيّر السطر إلى DECLARE @Apply bit = 1 ثم شغّل السكربت كاملًا.
   Preview first (@Apply=0); set @Apply=1 to save normalized areas, including unknown names as اخرى.
   Approved 40 unique area labels; المرج الجديده maps to المرج الجديدة.
   Includes archived students; does not rewrite historical requests.
   Reviewed neighborhood aliases map to approved areas (including منية السيرج to اخرى).
   Any other nonempty unrecognized area maps to اخرى. Missing/blank areas stay missing.
   Optional @ActorId identifies the administrator; NULL records a system correction.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Apply bit = 0;
DECLARE @ActorId uniqueidentifier = NULL;
DECLARE @Now datetime2 = SYSUTCDATETIME();
DECLARE @Correlation nvarchar(64) = CONVERT(nvarchar(36),NEWID());
DECLARE @ActorName nvarchar(200) = COALESCE((SELECT FullName FROM dbo.AspNetUsers WHERE Id=@ActorId),N'توحيد المناطق');
IF @ActorId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.AspNetUsers WHERE Id=@ActorId) THROW 51000,N'معرف المسؤول غير موجود.',1;

DROP TABLE IF EXISTS #AreaAliases;
DROP TABLE IF EXISTS #AreaPlan;
CREATE TABLE #AreaAliases (AliasKey nvarchar(100) COLLATE Latin1_General_100_BIN2 PRIMARY KEY,Canonical nvarchar(100) NOT NULL);
INSERT #AreaAliases(AliasKey,Canonical) VALUES
(N'ابراهيمبك',N'ابراهيم بك'),
(N'ابنالحكم',N'ابن الحكم'),
(N'ارضنوبار',N'ارض نوبار'),
(N'الاسكانالصناعي',N'الاسكان الصناعي'),
(N'التعاون',N'التعاون'),
(N'الخانكه',N'الخانكة'),
(N'الزيتون',N'الزيتون'),
(N'العبور',N'العبور'),
(N'الفرنواني',N'الفرنواني'),
(N'القناطرالخيريه',N'القناطر الخيرية'),
(N'الكورنيش',N'الكورنيش'),
(N'المرج',N'المرج'),
(N'المرجالجديده',N'المرج الجديدة'),
(N'المظلات',N'المظلات'),
(N'المعاديالجديده',N'المعادى الجديدة'),
(N'الملف',N'الملف'),
(N'المنشيه',N'المنشية'),
(N'المنشيهالجديده',N'المنشية الجديدة'),
(N'النزههالجديده',N'النزهه الجديده'),
(N'الوحده',N'الوحدة'),
(N'امبيومي',N'ام بيومي'),
(N'باسوس',N'باسوس'),
(N'بهتيم',N'بهتيم'),
(N'جامعابوالهنا',N'جامع ابو الهنا'),
(N'حدائقالاهرام',N'حدائق الاهرام'),
(N'شبراالبلد',N'شبرا البلد'),
(N'شبرامصر',N'شبرا مصر'),
(N'عبود',N'عبود'),
(N'عزبهعثمان',N'عزبة عثمان'),
(N'عزبهرستم',N'عزبه رستم'),
(N'قهوهشرف',N'قهوة شرف'),
(N'كليهالزراعه',N'كلية الزراعة'),
(N'كوبريعرابي',N'كوبرى عرابى'),
(N'مدينهالسلام',N'مدينة السلام'),
(N'مساكنالصباغه',N'مساكن الصباغة'),
(N'مساكنالضباط',N'مساكن الضباط'),
(N'مساكنحجازي',N'مساكن حجازى'),
(N'مسطرد',N'مسطرد'),
(N'منطي',N'منطي'),
(N'اخري',N'اخرى'),
(N'شبراالمحطه',N'شبرا البلد'),
(N'شبراالخيمه',N'شبرا البلد'),
(N'ارضنوبارشبراالخيمه',N'ارض نوبار'),
(N'المساكن',N'الوحدة'),
(N'الملفشبراالخيمه',N'الملف'),
(N'الوحدهعندالمصانع',N'الوحدة'),
(N'خلفالكنيسه',N'شبرا البلد'),
(N'شارعزكريا',N'شبرا البلد'),
(N'منيهالسيرج',N'اخرى');
SELECT Id,FullName,IsDeleted,Area AS Previous,CONVERT(binary(8),Version) AS PreviousVersion,
       CONVERT(nvarchar(max),LOWER(COALESCE(Area,N''))) COLLATE Latin1_General_100_BIN2 AS AliasKey,
       CONVERT(nvarchar(100),NULL) AS Canonical
INTO #AreaPlan FROM dbo.Students;
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(32),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(9),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(13),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(10),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(160),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(8206),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(8207),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1564),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(8203),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1600),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1648),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1611),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1612),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1613),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1614),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1615),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1616),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1617),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1618),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1619),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1620),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1621),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1622),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1623),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1624),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1625),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1626),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1627),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1628),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1629),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1630),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,NCHAR(1631),N'');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,N'أ',N'ا');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,N'إ',N'ا');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,N'آ',N'ا');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,N'ٱ',N'ا');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,N'ى',N'ي');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,N'ی',N'ي');
UPDATE #AreaPlan SET AliasKey=REPLACE(AliasKey,N'ة',N'ه');
UPDATE p SET Canonical=m.Canonical FROM #AreaPlan p JOIN #AreaAliases m ON m.AliasKey=p.AliasKey;
UPDATE #AreaPlan SET Canonical=N'اخرى' WHERE Canonical IS NULL AND AliasKey<>N'';
-- Preview all changes, including fallback to اخرى. Empty values are not assigned an area.
SELECT Id,FullName,IsDeleted,Previous AS [المنطقة الحالية],Canonical AS [المنطقة الموحدة]
FROM #AreaPlan WHERE Canonical IS NOT NULL AND CONVERT(varbinary(max),COALESCE(Previous,N''))<>CONVERT(varbinary(max),Canonical) ORDER BY FullName,Id;
SELECT DB_NAME() AS [قاعدة البيانات],
       CASE WHEN @Apply=0 THEN N'معاينة فقط — لم يتم حفظ أي تغيير' ELSE N'حفظ التغييرات' END AS [وضع التنفيذ],
       COUNT_BIG(*) AS [عدد المخدومين المطلوب تعديلهم],
       COALESCE(SUM(CONVERT(bigint,CASE WHEN Canonical=N'اخرى' THEN 1 ELSE 0 END)),0) AS [عدد التحويلات إلى اخرى]
FROM #AreaPlan WHERE Canonical IS NOT NULL AND CONVERT(varbinary(max),COALESCE(Previous,N''))<>CONVERT(varbinary(max),Canonical);
IF @Apply=0
BEGIN
 PRINT N'معاينة فقط: لحفظ التغييرات غيّر DECLARE @Apply bit = 0 إلى DECLARE @Apply bit = 1 ثم شغّل السكربت كاملًا.';
 RETURN;
END;

BEGIN TRY
 BEGIN TRANSACTION;
 -- Fail rather than overwrite a record edited since the preview snapshot.
 IF EXISTS(SELECT 1 FROM #AreaPlan p JOIN dbo.Students s WITH(UPDLOCK,HOLDLOCK) ON s.Id=p.Id
           WHERE p.Canonical IS NOT NULL AND CONVERT(varbinary(max),COALESCE(p.Previous,N''))<>CONVERT(varbinary(max),p.Canonical) AND s.Version<>p.PreviousVersion)
     THROW 51001,N'تغيرت بيانات بعض المخدومين. أعد تشغيل المعاينة قبل الحفظ.',1;
 DECLARE @Changed TABLE(Id uniqueidentifier,Previous nvarchar(max),Canonical nvarchar(100));
 UPDATE s SET Area=p.Canonical,UpdatedAt=@Now
 OUTPUT inserted.Id,deleted.Area,inserted.Area INTO @Changed
 FROM dbo.Students s JOIN #AreaPlan p ON p.Id=s.Id
 WHERE p.Canonical IS NOT NULL AND CONVERT(varbinary(max),COALESCE(p.Previous,N''))<>CONVERT(varbinary(max),p.Canonical);
 INSERT dbo.StudentEditLogs(Id,StudentId,UpdatedAt,UpdatedByUserId,UpdatedByUserName,Details)
 SELECT NEWID(),Id,@Now,COALESCE(@ActorId,'00000000-0000-0000-0000-000000000000'),@ActorName,
        CONCAT(N'توحيد المنطقة من «',Previous,N'» إلى «',Canonical,N'».') FROM @Changed;
 INSERT dbo.AuditEntry(Id,ActorId,Timestamp,EntityType,EntityId,Action,BeforeJson,AfterJson,CorrelationId,Reason)
 SELECT NEWID(),@ActorId,@Now,N'Student',CONVERT(nvarchar(64),c.Id),N'Modified',
        (SELECT c.Previous AS Area FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
        (SELECT c.Canonical AS Area FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
        @Correlation,N'توحيد المناطق' FROM @Changed c;
 COMMIT;
 SELECT DB_NAME() AS [قاعدة البيانات],N'تم حفظ التغييرات' AS [نتيجة التنفيذ],
        COUNT_BIG(*) AS [عدد المخدومين الذين تم توحيد منطقتهم],
        COALESCE(SUM(CONVERT(bigint,CASE WHEN Canonical=N'اخرى' THEN 1 ELSE 0 END)),0) AS [عدد المخدومين الذين تم تحويلهم إلى اخرى]
 FROM @Changed;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
