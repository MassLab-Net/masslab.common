using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Victor.Common.Storage.Abstractions;
using Victor.Common.Storage.FileSystem.Configuration;
using Victor.Common.Storage.FileSystem.Services;

namespace Victor.Common.Storage.FileSystem.Extensions;

/// <summary>Registration helpers for file-system blob storage.</summary>
public static class FileSystemStorageExtensions
{
    /// <summary>Registers file-system storage as <see cref="IBlobStorage"/>.</summary>
    public static IServiceCollection AddFileSystemBlobStorage(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = FileSystemStorageOptions.SectionName)
    {
        var options = new FileSystemStorageOptions();
        configuration?.GetSection(sectionName).Bind(options);
        Validate(options);

        if (configuration is not null)
            services.Configure<FileSystemStorageOptions>(configuration.GetSection(sectionName));
        else
            services.Configure<FileSystemStorageOptions>(_ => { });

        services.AddSingleton<IBlobStorage, FileSystemBlobStorage>();
        return services;
    }

    private static void Validate(FileSystemStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.RootPath))
            throw new ArgumentException("Root path is required.", nameof(options.RootPath));
        if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl)
            && !Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out _))
            throw new ArgumentException("Public base URL must be an absolute URI.", nameof(options.PublicBaseUrl));
    }
}
