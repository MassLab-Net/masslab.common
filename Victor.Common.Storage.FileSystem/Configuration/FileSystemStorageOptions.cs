namespace Victor.Common.Storage.FileSystem.Configuration;

/// <summary>Options for file-system storage.</summary>
public class FileSystemStorageOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Storage:FileSystem";

    /// <summary>Root folder for persisted blobs.</summary>
    public string RootPath { get; set; } = "storage";

    /// <summary>Base URL used when producing signed URLs.</summary>
    public string? PublicBaseUrl { get; set; }
}
