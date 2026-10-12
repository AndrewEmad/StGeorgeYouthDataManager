using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Auth.Service.Abstractions.Commands;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.WebApi.BackgroundWorkers;
using YouthDataManager.WebApi.Controllers;

internal sealed class BenchmarkApiFactory(string connection):WebApplicationFactory<AuthController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing").UseSetting("Database:InitializeOnStartup","false").UseSetting("NotificationWorker:Enabled","false");
        builder.UseContentRoot(Path.GetFullPath("4.WebApis/YouthDataManager.WebApi"));
        builder.ConfigureLogging(logging=>logging.ClearProviders());
        builder.ConfigureServices(services=>{
            services.RemoveAll<DbContextOptions<AppDbContext>>();services.AddDbContext<AppDbContext>(o=>o.UseSqlServer(connection));
            foreach(var worker in services.Where(s=>s.ServiceType==typeof(IHostedService) && s.ImplementationType==typeof(NotificationReminderWorker)).ToArray())services.Remove(worker);
        });
    }
}
internal static class ApiBenchmark
{
    public static async Task<(List<object> Results,bool Passed)> Run(string connection,IEnumerable<string>? operations=null)
    {
        await using var factory=new BenchmarkApiFactory(connection);var clients=new List<HttpClient>();
        try
        {
            await using(var scope=factory.Services.CreateAsyncScope())
            {
                var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                if(!await roles.RoleExistsAsync("Manager") && !(await roles.CreateAsync(new IdentityRole<Guid>("Manager"))).Succeeded)throw new InvalidOperationException("Could not seed benchmark role.");
                var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                for(var i=0;i<20;i++)
                {
                    var user=new ApplicationUser {Id=Guid.NewGuid(),UserName="http_benchmark_"+i,FullName="HTTP Manager "+i,Phone="01012345678",Address="Cairo",MustChangePassword=false};
                    if(!(await users.CreateAsync(user,"Benchmark_password123!")).Succeeded || !(await users.AddToRoleAsync(user,"Manager")).Succeeded)throw new InvalidOperationException("Could not seed benchmark identity.");
                    var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});var issued=await scope.ServiceProvider.GetRequiredService<IAuthCommandsService>().Issue(user,Guid.NewGuid());
                    if(issued.Status!=ServiceResultStatus.Success)throw new InvalidOperationException("Could not issue benchmark session.");
                    var login=issued.Data!;
                    if(login.RequiresPasswordChange || login.RequiresProfileCompletion)throw new InvalidOperationException("Benchmark identity is restricted.");
                    client.DefaultRequestHeaders.Authorization=new("Bearer",login.Token);clients.Add(client);
                }
            }
            var results=new List<object>();var passed=true;var requested=operations?.ToHashSet(StringComparer.Ordinal);
            foreach(var operation in new[]{"list","contact","workload","workload-owners","data-quality","attendance","follow-up","manager"}.Where(o=>requested==null || requested.Contains(o)))
            {
                var route=operation=="list" ? "/api/StudentQueries/paged?page=1&pageSize=25" : operation=="manager" ? "/api/reports/dashboard/manager" : "/api/reports/suite/"+operation+"?page=1&pageSize=25";
                async Task<double> Measure(HttpClient client){var watch=Stopwatch.StartNew();using var response=await client.GetAsync(route);response.EnsureSuccessStatusCode();await response.Content.ReadAsByteArrayAsync();return watch.Elapsed.TotalMilliseconds;}
                await Measure(clients[0]);
                var samples=(await Task.WhenAll(clients.Select(async client=>{var rows=new List<double>();for(var i=0;i<3;i++)rows.Add(await Measure(client));return rows;}))).SelectMany(x=>x).Order().ToArray();
                var p95=samples[(int)Math.Ceiling(samples.Length*.95)-1];var limit=operation=="list" ? 500 : 1000;passed &= p95<limit;
                results.Add(new {Operation=operation,Transport="ASP.NET TestServer HTTP",AuthenticatedUsers=20,Samples=samples.Length,P50Ms=Math.Round(samples[samples.Length/2],1),P95Ms=Math.Round(p95,1),MaxMs=Math.Round(samples[^1],1),TargetMs=limit,Passed=p95<limit});
                Console.WriteLine($"HTTP {operation}: p95 {p95:F1} ms, 20 authenticated users");
            }
            // Mixed pages and areas exercise distinct SQL requests rather than only overlapping identical reads.
            foreach(var operation in new[]{"contact","workload","attendance","follow-up"}.Where(o=>requested==null || requested.Contains(o)))
            {
                async Task<double> Measure(HttpClient client,int index){var route="/api/reports/suite/"+operation+"?page="+(index/10+1)+"&pageSize=25&area="+Uri.EscapeDataString("Area "+index%10);var watch=Stopwatch.StartNew();using var response=await client.GetAsync(route);response.EnsureSuccessStatusCode();await response.Content.ReadAsByteArrayAsync();return watch.Elapsed.TotalMilliseconds;}
                await Measure(clients[0],0);
                var samples=(await Task.WhenAll(clients.Select(async(client,index)=>{var rows=new List<double>();for(var i=0;i<3;i++)rows.Add(await Measure(client,index));return rows;}))).SelectMany(x=>x).Order().ToArray();
                var p95=samples[(int)Math.Ceiling(samples.Length*.95)-1];passed &= p95<1000;
                results.Add(new {Operation=operation+"-mixed-filters",Transport="ASP.NET TestServer HTTP",AuthenticatedUsers=20,DistinctQueries=20,Samples=samples.Length,P50Ms=Math.Round(samples[samples.Length/2],1),P95Ms=Math.Round(p95,1),MaxMs=Math.Round(samples[^1],1),TargetMs=1000,Passed=p95<1000});
                Console.WriteLine($"HTTP {operation} mixed filters: p95 {p95:F1} ms, 20 authenticated users");
            }
            if(requested==null || requested.Contains("attendance")){
                await using var scope=factory.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var ownerIds=await db.UserRoles.Where(ur=>db.Roles.Any(r=>r.Id==ur.RoleId && r.Name=="Servant")).Select(ur=>ur.UserId).OrderBy(id=>id).Take(20).ToArrayAsync();
                if(ownerIds.Length!=20)throw new InvalidOperationException("Benchmark requires twenty distinct assignment owners.");
                async Task<double> Measure(HttpClient client,int index){var watch=Stopwatch.StartNew();using var response=await client.GetAsync("/api/reports/suite/attendance?page=1&pageSize=25&servantId="+ownerIds[index]);response.EnsureSuccessStatusCode();await response.Content.ReadAsByteArrayAsync();return watch.Elapsed.TotalMilliseconds;}
                await Measure(clients[0],0);var samples=(await Task.WhenAll(clients.Select(async(client,index)=>{var rows=new List<double>();for(var i=0;i<3;i++)rows.Add(await Measure(client,index));return rows;}))).SelectMany(x=>x).Order().ToArray();var p95=samples[(int)Math.Ceiling(samples.Length*.95)-1];passed &= p95<1000;
                results.Add(new {Operation="attendance-mixed-owners",Transport="ASP.NET TestServer HTTP",AuthenticatedUsers=20,DistinctQueries=20,Samples=samples.Length,P50Ms=Math.Round(samples[samples.Length/2],1),P95Ms=Math.Round(p95,1),MaxMs=Math.Round(samples[^1],1),TargetMs=1000,Passed=p95<1000});Console.WriteLine($"HTTP attendance mixed owners: p95 {p95:F1} ms, 20 authenticated users");
            }
            return (results,passed);
        }
        finally {foreach(var client in clients)client.Dispose();}
    }
}
