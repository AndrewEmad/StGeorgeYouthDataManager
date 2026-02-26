namespace YouthDataManager.Photo.Service.Abstractions;

public class PhotoUploadOptions
{
    public const string SectionName = "PhotoUpload";

    public long MaxUploadBytes { get; set; } = 5_242_880; // 5MB
    public int MaxDimensionPx { get; set; } = 1200;
    public int JpegQuality { get; set; } = 92;
    public string UploadBasePath { get; set; } = "uploads/photos";
}
