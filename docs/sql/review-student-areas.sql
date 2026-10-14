/* SQL Server: run against the YouthDataManager database.
   Read-only inventory to agree on dropdown labels and spelling mappings.
   Includes archived students. Does not merge names or change any data.
   Binary collation and byte length keep spelling/spacing differences visible.
*/
SET NOCOUNT ON;

SELECT
    Area COLLATE Latin1_General_100_BIN2 AS [المنطقة الحالية],
    N'[' + COALESCE(Area COLLATE Latin1_General_100_BIN2,N'<NULL>') + N']' AS [القيمة مع إظهار حدود المسافات],
    COUNT_BIG(*) AS [عدد المخدومين],
    SUM(CONVERT(bigint,CASE WHEN IsDeleted=0 THEN 1 ELSE 0 END)) AS [غير المؤرشفين],
    SUM(CONVERT(bigint,CASE WHEN IsDeleted=1 THEN 1 ELSE 0 END)) AS [المؤرشفين]
FROM dbo.Students
GROUP BY Area COLLATE Latin1_General_100_BIN2,DATALENGTH(Area)
ORDER BY COUNT_BIG(*) DESC,Area COLLATE Latin1_General_100_BIN2,DATALENGTH(Area);
