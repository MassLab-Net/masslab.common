using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Victor.Common.Storage.AzureBlob.Extensions;
using Victor.Common.Storage.FileSystem.Configuration;
using Victor.Common.Storage.FileSystem.Extensions;
using Victor.Common.Storage.FileSystem.Services;
using Victor.Common.Storage.Models;
using Victor.Common.Storage.S3.Extensions;

namespace Victor.Common.Storage.Tests;

public class FileSystemBlobStorageTests
{
    [Fact]
    public async Task File_system_provider_uploads_downloads_and_deletes_blob()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var storage = new FileSystemBlobStorage(Options.Create(new FileSystemStorageOptions { RootPath = root }));
        await using var payload = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

        var info = await storage.UploadAsync("docs", "hello.txt", payload, new BlobUploadOptions { ContentType = "text/plain" });
        await using var download = await storage.DownloadAsync("docs", "hello.txt");
        var signed = await storage.GetSignedUrlAsync("docs", "hello.txt", TimeSpan.FromMinutes(5));
        await storage.DeleteAsync("docs", "hello.txt");

        info.Length.Should().Be(5);
        download.Should().NotBeNull();
        new StreamReader(download!.Content).ReadToEnd().Should().Be("hello");
        signed.IsFile.Should().BeTrue();
        (await storage.DownloadAsync("docs", "hello.txt")).Should().BeNull();
    }

    [Fact]
    public async Task File_system_provider_rejects_empty_blob_reference()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var storage = new FileSystemBlobStorage(Options.Create(new FileSystemStorageOptions { RootPath = root }));
        await using var payload = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

        var act = () => storage.UploadAsync("docs", "", payload);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public async Task File_system_provider_rejects_non_positive_signed_url_lifetime()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var storage = new FileSystemBlobStorage(Options.Create(new FileSystemStorageOptions { RootPath = root }));

        var act = () => storage.GetSignedUrlAsync("docs", "hello.txt", TimeSpan.Zero);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("lifetime");
    }

    [Fact]
    public void File_system_registration_rejects_invalid_options()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:FileSystem:RootPath"] = ""
            })
            .Build();

        var act = () => services.AddFileSystemBlobStorage(configuration);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(FileSystemStorageOptions.RootPath));
    }

    [Fact]
    public void Azure_registration_rejects_missing_connection_settings()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var act = () => services.AddAzureBlobStorage(configuration);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void S3_registration_rejects_partial_credentials()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:S3:AccessKey"] = "key"
            })
            .Build();

        var act = () => services.AddS3BlobStorage(configuration);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("AccessKey");
    }
}
