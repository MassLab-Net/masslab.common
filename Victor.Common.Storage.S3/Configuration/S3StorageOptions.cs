namespace Victor.Common.Storage.S3.Configuration;

/// <summary>Options for S3-compatible blob storage.</summary>
public class S3StorageOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Storage:S3";

    /// <summary>AWS access key. When empty, the default AWS credential chain is used.</summary>
    public string? AccessKey { get; set; }

    /// <summary>AWS secret key. When empty, the default AWS credential chain is used.</summary>
    public string? SecretKey { get; set; }

    /// <summary>AWS region system name.</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>Optional S3-compatible service URL, for example MinIO.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Use path-style bucket addressing.</summary>
    public bool ForcePathStyle { get; set; }
}
