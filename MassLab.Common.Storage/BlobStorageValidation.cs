namespace MassLab.Common.Storage;

/// <summary>Shared validation helpers for blob storage providers.</summary>
public static class BlobStorageValidation
{
    /// <summary>Ensures a container/name pair can identify a blob.</summary>
    public static void EnsureValidBlobReference(string container, string name)
    {
        if (string.IsNullOrWhiteSpace(container))
            throw new ArgumentException("Container is required.", nameof(container));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Blob name is required.", nameof(name));
    }

    /// <summary>Ensures a signed URL lifetime is usable.</summary>
    public static void EnsurePositiveLifetime(TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "Signed URL lifetime must be greater than zero.");
    }
}
