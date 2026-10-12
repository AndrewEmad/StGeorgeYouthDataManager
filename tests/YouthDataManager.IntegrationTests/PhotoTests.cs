using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Gif;
using YouthDataManager.Photo.Service.Abstractions;
using YouthDataManager.Photo.Service.Implementations;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.IntegrationTests;

public sealed class PhotoTests
{
    [Theory]
    [InlineData("jpeg")][InlineData("png")][InlineData("webp")][InlineData("gif")]
    public async Task Pinned_ImageSharp_decodes_valid_formats_and_preserves_previous_avatar_on_failure(string format)
    {
        var folder=Path.Combine(Path.GetTempPath(),"ydm-avatar-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            using var image=new Image<Rgba32>(16,12);using var bytes=new MemoryStream();
            await image.SaveAsync(bytes,format switch { "jpeg" => new JpegEncoder(),"png"=>new PngEncoder(),"webp"=>new WebpEncoder(),_ => (SixLabors.ImageSharp.Formats.IImageEncoder)new GifEncoder() });bytes.Position=0;
            var service=new PhotoUploadService(Options.Create(new PhotoUploadOptions {UploadBasePath=folder}),NullLogger<PhotoUploadService>.Instance);
            var id=Guid.NewGuid();var first=await service.ProcessAndSaveAsync(bytes,"image/"+format,"student",id);
            Assert.Equal(ServiceResultStatus.Success,first.Status);var path=Path.Combine(folder,first.Data!);var previous=await File.ReadAllBytesAsync(path);
            using var invalid=new MemoryStream([1,2,3]);Assert.Equal(ServiceResultStatus.Failure,(await service.ProcessAndSaveAsync(invalid,"image/jpeg","student",id)).Status);
            Assert.Equal(previous,await File.ReadAllBytesAsync(path));
        }
        finally { Directory.Delete(folder,true); }
    }
    [Fact]
    public async Task Orientation_is_applied_before_private_metadata_is_removed()
    {
        var folder=Path.Combine(Path.GetTempPath(),"ydm-avatar-orientation-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try{
            using var image=new Image<Rgba32>(20,10);image.Metadata.ExifProfile=new ExifProfile();image.Metadata.ExifProfile.SetValue(ExifTag.Orientation,(ushort)6);image.Metadata.ExifProfile.SetValue(ExifTag.ImageDescription,"Private camera metadata");using var bytes=new MemoryStream();await image.SaveAsJpegAsync(bytes);bytes.Position=0;
            var service=new PhotoUploadService(Options.Create(new PhotoUploadOptions{UploadBasePath=folder}),NullLogger<PhotoUploadService>.Instance);var result=await service.ProcessAndSaveAsync(bytes,"image/jpeg","student",Guid.NewGuid());Assert.Equal(ServiceResultStatus.Success,result.Status);using var saved=await Image.LoadAsync(Path.Combine(folder,result.Data!));Assert.Equal(10,saved.Width);Assert.Equal(20,saved.Height);Assert.Null(saved.Metadata.ExifProfile);Assert.Null(saved.Metadata.IccProfile);
        }finally{Directory.Delete(folder,true);}
    }
    [Fact]
    public async Task Animated_avatar_decodes_only_the_first_frame_and_rejects_excessive_frames()
    {
        var folder=Path.Combine(Path.GetTempPath(),"ydm-avatar-frames-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try{
            using var image=new Image<Rgba32>(4,4,new Rgba32(255,0,0));using var second=new Image<Rgba32>(4,4,new Rgba32(0,0,255));image.Frames.AddFrame(second.Frames.RootFrame);using var bytes=new MemoryStream();await image.SaveAsGifAsync(bytes);bytes.Position=0;
            var service=new PhotoUploadService(Options.Create(new PhotoUploadOptions{UploadBasePath=folder}),NullLogger<PhotoUploadService>.Instance);var result=await service.ProcessAndSaveAsync(bytes,"image/gif","student",Guid.NewGuid());Assert.Equal(ServiceResultStatus.Success,result.Status);using var saved=await Image.LoadAsync<Rgba32>(Path.Combine(folder,result.Data!));Assert.Single(saved.Frames);Assert.True(saved[0,0].R>240 && saved[0,0].B<20);
            while(image.Frames.Count<33)image.Frames.AddFrame(second.Frames.RootFrame);using var excessive=new MemoryStream();await image.SaveAsGifAsync(excessive);excessive.Position=0;var rejected=await service.ProcessAndSaveAsync(excessive,"image/gif","student",Guid.NewGuid());Assert.Equal(ServiceResultStatus.Failure,rejected.Status);Assert.Contains("frame",rejected.Message);
        }finally{Directory.Delete(folder,true);}
    }
    [Fact]
    public async Task Upload_bytes_and_image_dimensions_are_bounded_before_avatar_encoding()
    {
        var folder=Path.Combine(Path.GetTempPath(),"ydm-avatar-limits-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try{
            var service=new PhotoUploadService(Options.Create(new PhotoUploadOptions{UploadBasePath=folder,MaxUploadBytes=8}),NullLogger<PhotoUploadService>.Instance);using var bytes=new MemoryStream(new byte[16]);var rejected=await service.ProcessAndSaveAsync(bytes,"image/jpeg","student",Guid.NewGuid());Assert.Equal(ServiceResultStatus.Failure,rejected.Status);Assert.Contains("upload limit",rejected.Message);
            service=new PhotoUploadService(Options.Create(new PhotoUploadOptions{UploadBasePath=folder}),NullLogger<PhotoUploadService>.Instance);using var image=new Image<Rgba32>(8193,1);using var png=new MemoryStream();await image.SaveAsPngAsync(png);png.Position=0;rejected=await service.ProcessAndSaveAsync(png,"image/png","student",Guid.NewGuid());Assert.Equal(ServiceResultStatus.Failure,rejected.Status);Assert.Contains("pixel",rejected.Message);Assert.Empty(Directory.GetFiles(folder,"*",SearchOption.AllDirectories));
        }finally{Directory.Delete(folder,true);}
    }

}
