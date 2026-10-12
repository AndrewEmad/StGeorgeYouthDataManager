using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace YouthDataManager.Infrastructure.Data.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261011120000_ReviewedLegacyConstraints")]
public sealed class ReviewedLegacyConstraints : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM dbo.Students WHERE DATALENGTH(SecondaryPhone)>40) THROW 51002, 'Secondary phones exceed the validated limit. Review preflight results before deploying constraints.', 1;
IF EXISTS (SELECT 1 FROM dbo.StudentAssignmentRequests WHERE Status='Pending' GROUP BY StudentId,RequestedByUserId HAVING COUNT(*)>1) THROW 51002, 'Duplicate pending assignment requests require audited review.', 1;
IF EXISTS (SELECT 1 FROM dbo.StudentRemovalRequests WHERE Status='Pending' GROUP BY StudentId,RequestedByUserId HAVING COUNT(*)>1) THROW 51002, 'Duplicate pending removal requests require audited review.', 1;");
            migrationBuilder.DropIndex(
                name: "IX_StudentRemovalRequests_StudentId_RequestedByUserId",
                schema: "dbo",
                table: "StudentRemovalRequests");
            migrationBuilder.DropIndex(
                name: "IX_HomeVisits_ServantId",
                schema: "dbo",
                table: "HomeVisits");
            migrationBuilder.DropIndex(
                name: "IX_HomeVisits_StudentId",
                schema: "dbo",
                table: "HomeVisits");
            migrationBuilder.DropIndex(
                name: "IX_CallLogs_ServantId",
                schema: "dbo",
                table: "CallLogs");
            migrationBuilder.DropIndex(
                name: "IX_CallLogs_StudentId",
                schema: "dbo",
                table: "CallLogs");
            migrationBuilder.DropIndex(name:"IX_Students_NormalizedSecondaryPhone",schema:"dbo",table:"Students");
            migrationBuilder.DropColumn(name:"NormalizedSecondaryPhone",schema:"dbo",table:"Students");
            migrationBuilder.AlterColumn<string>(
                name: "SecondaryPhone",
                schema: "dbo",
                table: "Students",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
            migrationBuilder.AddColumn<string>(
                name: "NormalizedSecondaryPhone",
                schema: "dbo",
                table: "Students",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                computedColumnSql: "CONVERT(nvarchar(20),CASE WHEN LEN((CASE WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'+' THEN SUBSTRING(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),2,4000) WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),2)=N'00' THEN SUBSTRING(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),3,4000) WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'0' AND LEN(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''))=11 THEN N'2'+REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'1' AND LEN(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''))=10 THEN N'20'+REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') ELSE REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') END)) BETWEEN 7 AND 15 AND (CASE WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'+' THEN SUBSTRING(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),2,4000) WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),2)=N'00' THEN SUBSTRING(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),3,4000) WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'0' AND LEN(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''))=11 THEN N'2'+REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'1' AND LEN(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''))=10 THEN N'20'+REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') ELSE REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') END) COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^0-9]%' THEN (CASE WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'+' THEN SUBSTRING(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),2,4000) WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),2)=N'00' THEN SUBSTRING(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),3,4000) WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'0' AND LEN(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''))=11 THEN N'2'+REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'1' AND LEN(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''))=10 THEN N'20'+REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') ELSE REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') END) ELSE NULL END)",
                stored: true);
            migrationBuilder.CreateIndex(
                name: "IX_Students_NormalizedSecondaryPhone",
                schema: "dbo",
                table: "Students",
                column: "NormalizedSecondaryPhone",
                filter: "[IsDeleted] = 0");
            migrationBuilder.AlterColumn<DateTime>(
                name: "BirthDate",
                schema: "dbo",
                table: "Students",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_StudentRemovalRequests_StudentId_RequestedByUserId",
                schema: "dbo",
                table: "StudentRemovalRequests",
                columns: new[] { "StudentId", "RequestedByUserId" },
                unique: true,
                filter: "[Status] = 'Pending'");
            migrationBuilder.CreateIndex(
                name: "IX_StudentAssignmentRequests_StudentId_RequestedByUserId",
                schema: "dbo",
                table: "StudentAssignmentRequests",
                columns: new[] { "StudentId", "RequestedByUserId" },
                unique: true,
                filter: "[Status] = 'Pending'");
            migrationBuilder.CreateIndex(
                name: "IX_HomeVisits_ServantId_VisitDate",
                schema: "dbo",
                table: "HomeVisits",
                columns: new[] { "ServantId", "VisitDate" });
            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_ServantId_CallDate",
                schema: "dbo",
                table: "CallLogs",
                columns: new[] { "ServantId", "CallDate" });
    }
    protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropIndex(
                name: "IX_StudentRemovalRequests_StudentId_RequestedByUserId",
                schema: "dbo",
                table: "StudentRemovalRequests");
            migrationBuilder.DropIndex(
                name: "IX_StudentAssignmentRequests_StudentId_RequestedByUserId",
                schema: "dbo",
                table: "StudentAssignmentRequests");
            migrationBuilder.DropIndex(
                name: "IX_HomeVisits_ServantId_VisitDate",
                schema: "dbo",
                table: "HomeVisits");
            migrationBuilder.DropIndex(
                name: "IX_CallLogs_ServantId_CallDate",
                schema: "dbo",
                table: "CallLogs");
            migrationBuilder.DropIndex(name:"IX_Students_NormalizedSecondaryPhone",schema:"dbo",table:"Students");
            migrationBuilder.DropColumn(name:"NormalizedSecondaryPhone",schema:"dbo",table:"Students");
            migrationBuilder.AlterColumn<string>(
                name: "SecondaryPhone",
                schema: "dbo",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);
            migrationBuilder.AddColumn<string>(
                name: "NormalizedSecondaryPhone",
                schema: "dbo",
                table: "Students",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                computedColumnSql: "CONVERT(nvarchar(20),CASE WHEN LEN((CASE WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'+' THEN SUBSTRING(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),2,4000) WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),2)=N'00' THEN SUBSTRING(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),3,4000) WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'0' AND LEN(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''))=11 THEN N'2'+REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'1' AND LEN(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''))=10 THEN N'20'+REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') ELSE REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') END)) BETWEEN 7 AND 15 AND (CASE WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'+' THEN SUBSTRING(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),2,4000) WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),2)=N'00' THEN SUBSTRING(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),3,4000) WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'0' AND LEN(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''))=11 THEN N'2'+REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'1' AND LEN(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''))=10 THEN N'20'+REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') ELSE REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') END) COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^0-9]%' THEN (CASE WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'+' THEN SUBSTRING(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),2,4000) WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),2)=N'00' THEN SUBSTRING(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),3,4000) WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'0' AND LEN(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''))=11 THEN N'2'+REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') WHEN LEFT(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''),1)=N'1' AND LEN(REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N''))=10 THEN N'20'+REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') ELSE REPLACE(TRANSLATE([SecondaryPhone],N'\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9().-\t\r\n\u000b\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff',N'01234567890123456789                             '),N' ',N'') END) ELSE NULL END)",
                stored: true);
            migrationBuilder.CreateIndex(
                name: "IX_Students_NormalizedSecondaryPhone",
                schema: "dbo",
                table: "Students",
                column: "NormalizedSecondaryPhone",
                filter: "[IsDeleted] = 0");
            migrationBuilder.AlterColumn<DateTime>(
                name: "BirthDate",
                schema: "dbo",
                table: "Students",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_StudentRemovalRequests_StudentId_RequestedByUserId",
                schema: "dbo",
                table: "StudentRemovalRequests",
                columns: new[] { "StudentId", "RequestedByUserId" });
            migrationBuilder.CreateIndex(
                name: "IX_HomeVisits_ServantId",
                schema: "dbo",
                table: "HomeVisits",
                column: "ServantId");
            migrationBuilder.CreateIndex(
                name: "IX_HomeVisits_StudentId",
                schema: "dbo",
                table: "HomeVisits",
                column: "StudentId");
            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_ServantId",
                schema: "dbo",
                table: "CallLogs",
                column: "ServantId");
            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_StudentId",
                schema: "dbo",
                table: "CallLogs",
                column: "StudentId");
    }
}
