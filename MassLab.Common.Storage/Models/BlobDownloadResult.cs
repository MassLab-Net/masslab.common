namespace MassLab.Common.Storage.Models;

/// <summary>Downloaded blob payload and metadata.</summary>
public sealed class BlobDownloadResult : IAsyncDisposable
{
    /// <summary>Creates the result.</summary>
    public BlobDownloadResult(Stream content, BlobInfo info)
    {
        Content = content;
        Info = info;
    }

    /// <summary>Blob content stream.</summary>
    public Stream Content { get; }

    /// <summary>Blob metadata.</summary>
    public BlobInfo Info { get; }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
