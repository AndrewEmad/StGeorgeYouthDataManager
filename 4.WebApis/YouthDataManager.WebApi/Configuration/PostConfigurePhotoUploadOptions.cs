using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using YouthDataManager.Photo.Service.Abstractions;

namespace YouthDataManager.WebApi.Configuration;

public class PostConfigurePhotoUploadOptions : IPostConfigureOptions<PhotoUploadOptions>
{
    private readonly IWebHostEnvironment _env;

    public PostConfigurePhotoUploadOptions(IWebHostEnvironment env)
    {
        _env = env;
    }

    public void PostConfigure(string? name, PhotoUploadOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.UploadBasePath)) return;
        string path = options.UploadBasePath.Trim().TrimStart('/', '\\');
        options.UploadBasePath = Path.Combine(_env.ContentRootPath, path);
    }
}
