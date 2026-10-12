using System.Diagnostics;
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Infrastructure.Reports;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Implementations.Queries;

var server=Environment.GetEnvironmentVariable("YDM_BENCHMARK_SERVER") ?? "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true";
var name="YDM_Benchmark_"+Guid.NewGuid().ToString("N");var connection=server+";Database="+name+";MultipleActiveResultSets=false";
var caller=new BenchCaller();AppDbContext Context(QueryCounter? counter=null)=>new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection,o=>o.CommandTimeout(120)).AddInterceptors(counter ?? new QueryCounter()).Options,caller);
var output=Path.GetFullPath(args.FirstOrDefault() ?? "artifacts/benchmark.json");Directory.CreateDirectory(Path.GetDirectoryName(output)!);
await using var seed=Context();
try {
 Console.WriteLine("Migrating isolated benchmark database "+name+"...");await seed.Database.MigrateAsync();
 var role=new IdentityRole<Guid>("Servant"){Id=Guid.NewGuid(),NormalizedName="SERVANT"};seed.Roles.Add(role);
 var users=Enumerable.Range(0,50).Select(i=>new ApplicationUser{Id=Guid.NewGuid(),UserName="bench_"+i,NormalizedUserName="BENCH_"+i,FullName=i<2 ? "Same name" : "Owner "+i,NormalizedFullName="owner "+i,Phone="01012345678",Address="Cairo",IsActive=true}).ToArray();seed.Users.AddRange(users);seed.UserRoles.AddRange(users.Select(u=>new IdentityUserRole<Guid>{UserId=u.Id,RoleId=role.Id}));
 var students=Enumerable.Range(0,3000).Select(i=>new Student{Id=Guid.NewGuid(),FullName="Student "+i.ToString("D4"),NormalizedFullName="student "+i.ToString("D4"),Phone="010"+i.ToString("D8"),Area=i%20==0 ? "" : "Area "+i%10,AcademicYear="Year "+i%5,Address=i%10==0 ? "" : "Cairo",BirthMonth=i%12+1,ServantId=users[i%50].Id}).ToArray();seed.Students.AddRange(students);await seed.SaveChangesAsync();seed.ChangeTracker.Clear();
 Console.WriteLine("Seeding 100,000 calls, 25,000 visits and 104 weekly rosters...");
 await seed.Database.ExecuteSqlRawAsync(@"
SELECT ROW_NUMBER() OVER(ORDER BY Id)-1 n,Id,FullName,ServantId INTO #students FROM dbo.Students;
SELECT ROW_NUMBER() OVER(ORDER BY Id)-1 n,Id INTO #owners FROM dbo.AspNetUsers;
WITH numbers AS(SELECT TOP (100000) ROW_NUMBER() OVER(ORDER BY (SELECT NULL))-1 n FROM sys.all_objects a CROSS JOIN sys.all_objects b)
INSERT dbo.CallLogs(Id,StudentId,ServantId,CallDate,CallStatus,Notes,CreatedAt,IsVoided)
SELECT NEWID(),s.Id,u.Id,DATEADD(minute,-n.n*10,SYSUTCDATETIME()),n.n%5,N'Benchmark',SYSUTCDATETIME(),0 FROM numbers n JOIN #students s ON s.n=n.n%3000 JOIN #owners u ON u.n=n.n%50;
WITH numbers AS(SELECT TOP (25000) ROW_NUMBER() OVER(ORDER BY (SELECT NULL))-1 n FROM sys.all_objects a CROSS JOIN sys.all_objects b)
INSERT dbo.HomeVisits(Id,StudentId,ServantId,VisitDate,VisitOutcome,Notes,CreatedAt,IsVoided)
SELECT NEWID(),s.Id,u.Id,DATEADD(minute,-n.n*40,SYSUTCDATETIME()),n.n%4,N'Benchmark',SYSUTCDATETIME(),0 FROM numbers n JOIN #students s ON s.n=n.n%3000 JOIN #owners u ON u.n=n.n%50;
INSERT dbo.HomeVisitParticipants(HomeVisitId,ServantId) SELECT Id,ServantId FROM dbo.HomeVisits WHERE VisitOutcome=0;
WITH numbers AS(SELECT TOP (104) ROW_NUMBER() OVER(ORDER BY (SELECT NULL))-1 n FROM sys.all_objects)
INSERT dbo.MeetingSession(Id,Date,Status,RosterKnown,CreatedAt,UpdatedAt,FinalizedAt) SELECT NEWID(),DATEADD(week,n,CAST('20241011' AS date)),'Finalized',1,SYSUTCDATETIME(),SYSUTCDATETIME(),SYSUTCDATETIME() FROM numbers;
INSERT dbo.MeetingRosterEntry(Id,SessionId,PersonId,Kind,Name,AssignedServantId,Present) SELECT NEWID(),m.Id,s.Id,'Student',s.FullName,s.ServantId,CASE WHEN s.n%7=0 THEN 0 ELSE 1 END FROM dbo.MeetingSession m CROSS JOIN #students s;
INSERT dbo.MeetingRosterEntry(Id,SessionId,PersonId,Kind,Name,AssignedServantId,Present) SELECT NEWID(),m.Id,u.Id,'Servant','Owner',NULL,1 FROM dbo.MeetingSession m CROSS JOIN #owners u;
INSERT dbo.FollowUpTask(Id,StudentId,AssigneeId,Type,Status,DueAt,CreatedAt) SELECT NEWID(),Id,ServantId,'Call','Open',DATEADD(day,-n%30,SYSUTCDATETIME()),SYSUTCDATETIME() FROM #students;
DROP TABLE #students;DROP TABLE #owners;");

 var results=new List<object>();var passed=true;
 foreach(var operation in (args.Length>1 ? args.Skip(1) : new[]{"list","contact","workload","workload-owners","data-quality","attendance","follow-up","manager"})) {
  Console.WriteLine("Measuring "+operation+" with 20 concurrent callers...");
  async Task<(double Ms,int Queries)> Run(){var counter=new QueryCounter();await using var db=Context(counter);var store=new ApplicationStore(db);var report=new ReportDataProvider(db,caller,new ConfigurationBuilder().Build());var watch=Stopwatch.StartNew();
   if(operation=="list")await new StudentQueriesService(store,caller).GetPaged(new(null,null,null,null,null,null,null,null),1,25);
   else if(operation=="manager")await report.ManagerDashboard();else await report.Suite(operation,new(){PageSize=25});
   return (watch.Elapsed.TotalMilliseconds,counter.Count);
  }
  await Run();var samples=(await Task.WhenAll(Enumerable.Range(0,20).Select(async _=>{var values=new List<(double Ms,int Queries)>();for(var i=0;i<3;i++)values.Add(await Run());return values;}))).SelectMany(x=>x).OrderBy(x=>x.Ms).ToArray();
  var p95=samples[(int)Math.Ceiling(samples.Length*.95)-1].Ms;var limit=operation=="list" ? 500 : 1000;
  passed &= p95<limit;
  results.Add(new {Operation=operation,ConcurrentCallers=20,Samples=samples.Length,P50Ms=Math.Round(samples[samples.Length/2].Ms,1),P95Ms=Math.Round(p95,1),MaxMs=Math.Round(samples[^1].Ms,1),MinQueries=samples.Min(x=>x.Queries),MaxQueries=samples.Max(x=>x.Queries),TargetMs=limit,Passed=p95<limit});
  Console.WriteLine($"{operation}: p95 {p95:F1} ms; {samples.Min(x=>x.Queries)}–{samples.Max(x=>x.Queries)} queries");
 }
 var http=await ApiBenchmark.Run(connection,args.Length>1 ? args.Skip(1) : null);passed &= http.Passed;
 await seed.Database.OpenConnectionAsync();await using var serverInfo=seed.Database.GetDbConnection().CreateCommand();serverInfo.CommandText="SELECT CONVERT(nvarchar(128),SERVERPROPERTY('Edition')),CONVERT(nvarchar(128),SERVERPROPERTY('ProductVersion')),compatibility_level FROM sys.databases WHERE name=DB_NAME()";
 string edition,sqlVersion;int compatibility;await using(var info=await serverInfo.ExecuteReaderAsync()){await info.ReadAsync();edition=info.GetString(0);sqlVersion=info.GetString(1);compatibility=info.GetByte(2);}
 await seed.Database.CloseConnectionAsync();
 await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new {SqlEdition=edition,SqlVersion=sqlVersion,Compatibility=compatibility,AvailableHostRamMiB=GC.GetGCMemoryInfo().TotalAvailableMemoryBytes/1024/1024,Architecture=System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),CapturedAt=DateTime.UtcNow,Runtime=Environment.Version.ToString(),OS=Environment.OSVersion.ToString(),Processors=Environment.ProcessorCount,Students=3000,Servants=50,Calls=100000,Visits=25000,WeeklyMeetings=104,RosterRows=317200,Results=results,HttpResults=http.Results},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("Results: "+output);
 await seed.Database.OpenConnectionAsync();await using var plans=seed.Database.GetDbConnection().CreateCommand();plans.CommandText="SELECT TOP(200) CONVERT(nvarchar(max),qp.query_plan) FROM sys.dm_exec_query_stats qs CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp WHERE qp.dbid=DB_ID() AND qp.query_plan IS NOT NULL ORDER BY qs.last_execution_time DESC";
 await using(var reader=await plans.ExecuteReaderAsync()){var captured=new HashSet<string>();while(await reader.ReadAsync())captured.Add(reader.GetString(0));await File.WriteAllTextAsync(Path.ChangeExtension(output,"plans.xml"),"<plans>"+string.Join("\n",captured)+"</plans>");}
 if(!passed)Environment.ExitCode=1;
} finally { await seed.Database.EnsureDeletedAsync();Console.WriteLine("Isolated benchmark database removed."); }
sealed class BenchCaller:ICallerContext{public Guid? UserId=>null;public bool IsPrivileged=>true;public string CorrelationId=>"benchmark";public string? Reason=>"Synthetic isolated data";private readonly DateTime now=DateTime.UtcNow;public DateTime UtcNow=>now;}
sealed class QueryCounter:DbCommandInterceptor{private int count;public int Count=>count;public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData eventData,InterceptionResult<DbDataReader> result,CancellationToken cancellationToken=default){Interlocked.Increment(ref count);return ValueTask.FromResult(result);}public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,CommandEventData eventData,InterceptionResult<int> result,CancellationToken cancellationToken=default){Interlocked.Increment(ref count);return ValueTask.FromResult(result);}}
