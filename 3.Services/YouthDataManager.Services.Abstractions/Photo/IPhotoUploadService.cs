using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Photo.Service.Abstractions;

public interface IPhotoUploadService
{
    /// <summary>
    /// Validates, resizes/compresses the image and saves to disk.
    /// Returns relative path (e.g. "servants/{id}.jpg") or failure.
    /// </summary>
    Task<ServiceResult<string>> ProcessAndSaveAsync(
        Stream stream,
        string contentType,
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);
}
