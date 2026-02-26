using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using YouthDataManager.Photo.Service.Abstractions;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Photo.Service.Implementations;

public class PhotoUploadService : IPhotoUploadService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private readonly PhotoUploadOptions _options;

    public PhotoUploadService(IOptions<PhotoUploadOptions> options)
    {
        _options = options.Value;
    }

    public async Task<ServiceResult<string>> ProcessAndSaveAsync(
        Stream stream,
        string contentType,
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        if (stream.CanSeek && stream.Length > _options.MaxUploadBytes)
            return ServiceResult<string>.Failure($"الملف يتجاوز الحد الأقصى المسموح ({_options.MaxUploadBytes / (1024 * 1024)} ميجابايت).");

        if (!AllowedContentTypes.Contains(contentType?.Trim() ?? ""))
            return ServiceResult<string>.Failure("نوع الملف غير مدعوم. استخدم صورة (JPEG، PNG، WebP أو GIF).");

        string folderName = entityType.Equals("servant", StringComparison.OrdinalIgnoreCase) ? "servants" : "students";
        string fileName = $"{entityId}.jpg";
        string relativePath = $"{folderName}/{fileName}";
        string fullDir = Path.Combine(_options.UploadBasePath, folderName);
        string fullPath = Path.Combine(fullDir, fileName);

        try
        {
            Directory.CreateDirectory(fullDir);

            using var image = await Image.LoadAsync(stream, cancellationToken);
            int max = _options.MaxDimensionPx;
            if (image.Width > max || image.Height > max)
            {
                double ratio = Math.Min((double)max / image.Width, (double)max / image.Height);
                int w = (int)(image.Width * ratio);
                int h = (int)(image.Height * ratio);
                image.Mutate(x => x.Resize(w, h, KnownResamplers.Lanczos3));
            }

            await using var outStream = File.Create(fullPath);
            var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
            {
                Quality = Math.Clamp(_options.JpegQuality, 1, 100)
            };
            await image.SaveAsJpegAsync(outStream, encoder, cancellationToken);
            return ServiceResult<string>.Success(relativePath);
        }
        catch (UnknownImageFormatException)
        {
            return ServiceResult<string>.Failure("الملف ليس صورة صالحة.");
        }
        catch (Exception ex)
        {
            return ServiceResult<string>.Failure(ex.Message);
        }
    }
}
