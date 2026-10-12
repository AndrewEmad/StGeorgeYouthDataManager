using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Photo.Service.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
namespace YouthDataManager.IntegrationTests;
sealed class NoMetadataSave : IUnitOfWork {public Task<int> SaveChangesAsync(CancellationToken ct=default)=>Task.FromResult(0);}
sealed class PhotoFailureFactory(string connection,string folder):IsolatedApiFactory(connection)
{
    protected override void ConfigureWebHost(IWebHostBuilder builder){base.ConfigureWebHost(builder);builder.ConfigureServices(s=>{s.RemoveAll<IUnitOfWork>();s.AddScoped<IUnitOfWork,NoMetadataSave>();s.Configure<PhotoUploadOptions>(o=>o.UploadBasePath=folder);});}
}
[Collection("SQL")]
public sealed class PhotoPersistenceTests(SqlFixture fixture)
{
    [Fact]
    public async Task Failed_metadata_save_returns_failure_and_preserves_previous_photo_reference_and_bytes()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var user=await fixture.User(p,"Manager");var db=p.GetRequiredService<AppDbContext>();var folder=Path.Combine(Path.GetTempPath(),"ydm-metadata-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);var old=Path.Combine(folder,"old-avatar.jpg");await File.WriteAllBytesAsync(old,[12,34,56]);
        try{
            var student=new Student {Id=Guid.NewGuid(),FullName="Photo",NormalizedFullName="photo",Phone="01012345678",PhotoPath="old-avatar.jpg"};db.Add(student);await db.SaveChangesAsync();await using var factory=new PhotoFailureFactory(fixture.Connection,folder);using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});var login=await client.PostAsJsonAsync("/api/Auth/login",new {username=user.UserName,password="password123"});login.EnsureSuccessStatusCode();client.DefaultRequestHeaders.Authorization=new("Bearer",(await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token);
            using var image=new Image<Rgba32>(20,20);using var bytes=new MemoryStream();await image.SaveAsPngAsync(bytes);bytes.Position=0;using var body=new MultipartFormDataContent();using var upload=new StreamContent(bytes);upload.Headers.ContentType=new("image/png");body.Add(upload,"file","avatar.png");Assert.Equal(HttpStatusCode.InternalServerError,(await client.PostAsync($"/api/StudentCommands/{student.Id}/photo",body)).StatusCode);
            db.ChangeTracker.Clear();Assert.Equal("old-avatar.jpg",(await db.Students.SingleAsync(s=>s.Id==student.Id)).PhotoPath);Assert.Equal(new byte[]{12,34,56},await File.ReadAllBytesAsync(old));
        }finally{Directory.Delete(folder,true);}
    }
}
