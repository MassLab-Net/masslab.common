using System.Security.Cryptography;
using System.Text;
using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Models;
using MassLab.Common.Email.Templates.FileSystem.Configuration;
using Microsoft.Extensions.Options;

namespace MassLab.Common.Email.Templates.FileSystem.Services;

public sealed class FileSystemEmailTemplateStore(IOptions<FileSystemEmailTemplateOptions> options) : IEmailTemplateStore
{
    private readonly FileSystemEmailTemplateOptions _options = options.Value;
    private readonly Dictionary<string, (DateTime LastWrite, EmailTemplate Template)> _cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<EmailTemplate?> GetAsync(string templateKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateKey) || templateKey.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || templateKey.Contains("..", StringComparison.Ordinal) || templateKey.Contains(Path.DirectorySeparatorChar) || templateKey.Contains(Path.AltDirectorySeparatorChar))
            throw new ArgumentException("Template key must be a simple folder name.", nameof(templateKey));

        var directory = Path.GetFullPath(Path.Combine(_options.RootPath, templateKey));
        var root = Path.GetFullPath(_options.RootPath);
        if (!directory.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Template key escapes the configured root path.", nameof(templateKey));

        var subjectPath = Path.Combine(directory, "subject.hbs");
        var htmlPath = Path.Combine(directory, "body.html.hbs");
        var textPath = Path.Combine(directory, "body.text.hbs");
        if (!File.Exists(subjectPath) || !File.Exists(htmlPath)) return null;

        var lastWrite = new[] { File.GetLastWriteTimeUtc(subjectPath), File.GetLastWriteTimeUtc(htmlPath), File.Exists(textPath) ? File.GetLastWriteTimeUtc(textPath) : DateTime.MinValue }.Max();
        lock (_cache)
        {
            if (_cache.TryGetValue(templateKey, out var cached) && (!_options.ReloadOnChange || cached.LastWrite == lastWrite))
                return cached.Template;
        }

        var subject = await File.ReadAllTextAsync(subjectPath, cancellationToken);
        var html = await File.ReadAllTextAsync(htmlPath, cancellationToken);
        var text = File.Exists(textPath) ? await File.ReadAllTextAsync(textPath, cancellationToken) : null;
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{subject}\n{html}\n{text}"))).ToLowerInvariant();
        var template = new EmailTemplate { Key = templateKey, Subject = subject, Html = html, Text = text, Fingerprint = fingerprint };
        lock (_cache) _cache[templateKey] = (lastWrite, template);
        return template;
    }
}
