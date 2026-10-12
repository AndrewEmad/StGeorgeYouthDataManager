using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Photo.Service.Abstractions;
using YouthDataManager.Photo.Service.Implementations;
using YouthDataManager.Shared.Service.Abstractions;

var server=Environment.GetEnvironmentVariable("YDM_RECOVERY_SQL_SERVER") ?? @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true";
var sourceName="YDM_RestoreSource_"+Guid.NewGuid().ToString("N");var restoredName="YDM_RestoreCheck_"+Guid.NewGuid().ToString("N");
var root=Path.GetFullPath(Path.Combine("artifacts","restore-verification-"+Guid.NewGuid().ToString("N")));Directory.CreateDirectory(root);var photos=Path.Combine(root,"photos");Directory.CreateDirectory(photos);var restoredPhotos=Path.Combine(root,"restored-photos");Directory.CreateDirectory(restoredPhotos);var backup=Path.Combine(root,"database.bak");
var sourceBuilder=new SqlConnectionStringBuilder(server){InitialCatalog=sourceName,MultipleActiveResultSets=false};var restoreBuilder=new SqlConnectionStringBuilder(server){InitialCatalog=restoredName,MultipleActiveResultSets=false};var masterBuilder=new SqlConnectionStringBuilder(server){InitialCatalog="master"};
await using var source=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(sourceBuilder.ConnectionString).Options);
await using var restored=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(restoreBuilder.ConnectionString).Options);
try{
 await source.Database.MigrateAsync();using var original=new Image<Rgba32>(64,48);using var bytes=new MemoryStream();await original.SaveAsPngAsync(bytes);bytes.Position=0;
 var photoService=new PhotoUploadService(Options.Create(new PhotoUploadOptions {UploadBasePath=photos}),NullLogger<PhotoUploadService>.Instance);var id=Guid.NewGuid();var photo=await photoService.ProcessAndSaveAsync(bytes,"image/png","student",id);if(photo.Status!=ServiceResultStatus.Success)throw new InvalidOperationException("Photo preparation failed.");
 source.Add(new Student {Id=id,FullName="Restore verification",NormalizedFullName="restore verification",Phone="01012345678",PhotoPath=photo.Data});await source.SaveChangesAsync();
 await using var connection=new SqlConnection(masterBuilder.ConnectionString);await connection.OpenAsync();
 await using(var command=connection.CreateCommand()){command.CommandTimeout=180;command.CommandText=$"BACKUP DATABASE [{sourceName}] TO DISK=@path WITH COPY_ONLY, CHECKSUM; RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM;";command.Parameters.AddWithValue("@path",backup);await command.ExecuteNonQueryAsync();}
 var logical=new List<(string Name,string Type)>();
 await using(var command=connection.CreateCommand()){command.CommandText="RESTORE FILELISTONLY FROM DISK=@path";command.Parameters.AddWithValue("@path",backup);await using var reader=await command.ExecuteReaderAsync();while(await reader.ReadAsync())logical.Add((reader.GetString(reader.GetOrdinal("LogicalName")),reader.GetString(reader.GetOrdinal("Type"))));}
 var dataPath=Path.GetDirectoryName((await source.Database.SqlQuery<string>($"SELECT physical_name AS Value FROM sys.database_files WHERE file_id=1").SingleAsync()))!;
 await using(var command=connection.CreateCommand()){
  command.CommandTimeout=180;command.Parameters.AddWithValue("@backup",backup);var moves=new List<string>();for(var i=0;i<logical.Count;i++){moves.Add("MOVE @logical"+i+" TO @file"+i);command.Parameters.AddWithValue("@logical"+i,logical[i].Name);command.Parameters.AddWithValue("@file"+i,Path.Combine(dataPath,restoredName+"-"+i+(logical[i].Type=="L" ? ".ldf" : ".mdf")));}
  command.CommandText=$"RESTORE DATABASE [{restoredName}] FROM DISK=@backup WITH "+string.Join(",",moves)+", CHECKSUM, RECOVERY";await command.ExecuteNonQueryAsync();
 }
 var hashes=new Dictionary<string,string>();foreach(var file in Directory.EnumerateFiles(photos,"*",SearchOption.AllDirectories)){var relative=Path.GetRelativePath(photos,file);var destination=Path.GetFullPath(Path.Combine(restoredPhotos,relative));if(!destination.StartsWith(restoredPhotos+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Photo escapes restore directory.");Directory.CreateDirectory(Path.GetDirectoryName(destination)!);File.Copy(file,destination);hashes[relative]=Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(file)));if(hashes[relative]!=Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(destination))))throw new InvalidOperationException("Photo hash mismatch.");}
 var student=await restored.Students.SingleAsync(s=>s.Id==id);var restoredFile=Path.Combine(restoredPhotos,student.PhotoPath!);using var avatar=await Image.LoadAsync(restoredFile);if(avatar.Width<1 || avatar.Height<1 || (await restored.Database.GetPendingMigrationsAsync()).Any())throw new InvalidOperationException("Restored data or image validation failed.");
 var result=new {VerifiedAt=DateTime.UtcNow,Runtime=Environment.Version.ToString(),SourceDatabase=sourceName,RestoredDatabase=restoredName,DatabaseChecksum=true,RestoredStudentId=id,PhotoCount=hashes.Count,PhotoHashes=hashes,ImageWidth=avatar.Width,ImageHeight=avatar.Height,ProductionTargetTested=false};await File.WriteAllTextAsync(Path.Combine(root,"result.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions {WriteIndented=true}));Console.WriteLine("Isolated database and persistent-photo restoration verified. Evidence: "+root);
}finally{
 // Both names are generated here; an operator cannot choose an existing database as the restore target.
 await restored.Database.EnsureDeletedAsync();await source.Database.EnsureDeletedAsync();
}
