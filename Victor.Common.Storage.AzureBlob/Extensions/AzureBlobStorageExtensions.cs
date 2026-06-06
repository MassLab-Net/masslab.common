using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Victor.Common.Storage.Abstractions;
using Victor.Common.Storage.AzureBlob.Configuration;
using Victor.Common.Storage.AzureBlob.Services;

namespace Victor.Common.Storage.AzureBlob.Extensions;

/// <summary>Registration helpers for Azure Blob storage.</summary>
public static class AzureBlobStorageExtensions
{
    /// <summary>Registers the Azure Blob provider.</summary>
    public static IServiceCollection AddAzureBlobStorage(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = AzureBlobStorageOptions.SectionName)
    {
        if (configuration is not null)
        {
            var configured = new AzureBlobStorageOptions();
            configuration.GetSection(sectionName).Bind(configured);
            Validate(configured);
        }

        if (configuration is not null)
            services.Configure<AzureBlobStorageOptions>(configuration.GetSection(sectionName));
        else
            services.Configure<AzureBlobStorageOptions>(_ => { });

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureBlobStorageOptions>>().Value;
            Validate(options);
            if (!string.IsNullOrWhiteSpace(options.ConnectionString))
                return new BlobServiceClient(options.ConnectionString);

            if (!string.IsNullOrWhiteSpace(options.ServiceUri))
                return new BlobServiceClient(new Uri(options.ServiceUri), new DefaultAzureCredential());

            throw new InvalidOperationException("Azure Blob Storage requires ConnectionString or ServiceUri.");
        });
        services.AddSingleton<IBlobStorage, AzureBlobStorage>();
        return services;
    }

    private static void Validate(AzureBlobStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString)
            && string.IsNullOrWhiteSpace(options.ServiceUri))
            throw new ArgumentException("Azure Blob Storage requires ConnectionString or ServiceUri.", nameof(options.ConnectionString));
        if (!string.IsNullOrWhiteSpace(options.ServiceUri)
            && !Uri.TryCreate(options.ServiceUri, UriKind.Absolute, out _))
            throw new ArgumentException("ServiceUri must be an absolute URI.", nameof(options.ServiceUri));
    }
}
