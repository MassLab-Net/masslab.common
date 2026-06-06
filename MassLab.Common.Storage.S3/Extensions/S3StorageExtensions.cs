using Amazon;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MassLab.Common.Storage.Abstractions;
using MassLab.Common.Storage.S3.Configuration;
using MassLab.Common.Storage.S3.Services;

namespace MassLab.Common.Storage.S3.Extensions;

/// <summary>Registration helpers for S3 blob storage.</summary>
public static class S3StorageExtensions
{
    /// <summary>Registers the S3 provider.</summary>
    public static IServiceCollection AddS3BlobStorage(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = S3StorageOptions.SectionName)
    {
        var configured = new S3StorageOptions();
        configuration?.GetSection(sectionName).Bind(configured);
        Validate(configured);

        if (configuration is not null)
            services.Configure<S3StorageOptions>(configuration.GetSection(sectionName));
        else
            services.Configure<S3StorageOptions>(_ => { });

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<S3StorageOptions>>().Value;
            Validate(options);
            var config = new AmazonS3Config
            {
                ForcePathStyle = options.ForcePathStyle
            };

            if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
                config.ServiceURL = options.ServiceUrl;
            else
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);

            return string.IsNullOrWhiteSpace(options.AccessKey) || string.IsNullOrWhiteSpace(options.SecretKey)
                ? new AmazonS3Client(config)
                : new AmazonS3Client(options.AccessKey, options.SecretKey, config);
        });
        services.AddSingleton<IBlobStorage, S3BlobStorage>();
        return services;
    }

    private static void Validate(S3StorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ServiceUrl) && string.IsNullOrWhiteSpace(options.Region))
            throw new ArgumentException("Region is required when ServiceUrl is not configured.", nameof(options.Region));
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl)
            && !Uri.TryCreate(options.ServiceUrl, UriKind.Absolute, out _))
            throw new ArgumentException("ServiceUrl must be an absolute URI.", nameof(options.ServiceUrl));
        if (string.IsNullOrWhiteSpace(options.AccessKey) != string.IsNullOrWhiteSpace(options.SecretKey))
            throw new ArgumentException("AccessKey and SecretKey must be configured together.", nameof(options.AccessKey));
    }
}
