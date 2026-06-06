namespace Victor.Common.Storage.AzureBlob.Configuration;

/// <summary>Options for Azure Blob Storage.</summary>
public class AzureBlobStorageOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Storage:AzureBlob";

    /// <summary>Azure Storage connection string.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Account service URI used with default Azure credentials.</summary>
    public string? ServiceUri { get; set; }
}
