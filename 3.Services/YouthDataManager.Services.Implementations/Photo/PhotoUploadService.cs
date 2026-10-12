using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using YouthDataManager.Photo.Service.Abstractions;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Photo.Service.Implementations;

public sealed class PhotoUploadService(IOptions<PhotoUploadOptions> options, ILogger<PhotoUploadService> logger) : IPhotoUploadService
{
    public async Task<ServiceResult<string>> ProcessAndSaveAsync(Stream stream, string contentType, string entityType, Guid entityId, CancellationToken cancellationToken=default)
    {
        var settings=options.Value;
        var folder=entityType.Equals("servant",StringComparison.OrdinalIgnoreCase) ? "servants" : "students";
        var name=$"{entityId}_{Guid.NewGuid():N}.jpg";
        var directory=Path.Combine(settings.UploadBasePath,folder);
        var temporary=Path.Combine(directory,name+".tmp");
        try
        {
            // Bound bytes even for streams without Length/Seek support.
            using var bytes=new MemoryStream(); var buffer=new byte[81920]; int count;
            while ((count=await stream.ReadAsync(buffer,cancellationToken))>0)
            {
                if (bytes.Length+count>settings.MaxUploadBytes) return ServiceResult<string>.Failure("Image exceeds the upload limit.");
                await bytes.WriteAsync(buffer.AsMemory(0,count),cancellationToken);
            }
            if (bytes.Length==0) return ServiceResult<string>.Failure("Empty image.");
            bytes.Position=0;
            var info=await Image.IdentifyAsync(new DecoderOptions { MaxFrames=33 },bytes,cancellationToken);
            var mime=info.Metadata.DecodedImageFormat?.DefaultMimeType;
            if (mime is not ("image/jpeg" or "image/png" or "image/webp" or "image/gif")) return ServiceResult<string>.Failure("Unsupported image format.");
            if (info.Width>8192 || info.Height>8192 || (long)info.Width*info.Height>24_000_000 || info.FrameMetadataCollection.Count>32)
                return ServiceResult<string>.Failure("Image exceeds pixel or frame limits.");
            bytes.Position=0;
            using var image=await Image.LoadAsync(new DecoderOptions { MaxFrames=1 },bytes,cancellationToken);
            image.Mutate(x => x.AutoOrient());
            if (image.Width>settings.MaxDimensionPx || image.Height>settings.MaxDimensionPx)
                image.Mutate(x => x.Resize(new ResizeOptions { Size=new Size(settings.MaxDimensionPx,settings.MaxDimensionPx),Mode=ResizeMode.Max }));
            image.Metadata.ExifProfile=null; image.Metadata.IccProfile=null; image.Metadata.XmpProfile=null; image.Metadata.IptcProfile=null;
            Directory.CreateDirectory(directory);
            await image.SaveAsJpegAsync(temporary,new JpegEncoder { Quality=Math.Clamp(settings.JpegQuality,1,100),SkipMetadata=true },cancellationToken);
            File.Move(temporary,Path.Combine(directory,name));
            // Versioned output leaves the old avatar intact until metadata commits.
            return ServiceResult<string>.Success($"{folder}/{name}");
        }
        catch (OperationCanceledException) { throw; }
        catch (UnknownImageFormatException) { return ServiceResult<string>.Failure("Invalid image bytes."); }
        catch (InvalidImageContentException) { return ServiceResult<string>.Failure("Invalid image content."); }
        catch (Exception ex)
        {
            logger.LogError(ex,"Avatar processing/storage failed for {EntityId}",entityId);
            return ServiceResult<string>.Failure("Unable to store the photo. Retry the photo upload.");
        }
        finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch (IOException ex) { logger.LogWarning(ex,"Temporary avatar cleanup failed for {EntityId}",entityId); } catch (UnauthorizedAccessException ex) { logger.LogWarning(ex,"Temporary avatar cleanup failed for {EntityId}",entityId); } }
    }
}
