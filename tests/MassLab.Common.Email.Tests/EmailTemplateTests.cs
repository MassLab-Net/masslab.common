using MassLab.Common.Email.Models;
using MassLab.Common.Email.Templates.FileSystem.Configuration;
using MassLab.Common.Email.Templates.FileSystem.Services;
using Microsoft.Extensions.Options;

namespace MassLab.Common.Email.Tests;

public class EmailTemplateTests
{
    [Fact]
    public async Task File_templates_render_html_encoded_values_and_fingerprint()
    {
        var root = Path.Combine(Path.GetTempPath(), "masslab-email-" + Guid.NewGuid());
        var folder = Path.Combine(root, "welcome");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "subject.hbs"), "Welcome {{name}}");
        await File.WriteAllTextAsync(Path.Combine(folder, "body.html.hbs"), "<p>{{name}}</p>{{#if active}}active{{/if}}");
        try
        {
            var store = new FileSystemEmailTemplateStore(Options.Create(new FileSystemEmailTemplateOptions { RootPath = root }));
            var template = await store.GetAsync("welcome");
            var rendered = await new HandlebarsEmailTemplateRenderer().RenderAsync(template!, new { name = "<Alice>", active = true });

            rendered.Subject.Should().Be("Welcome &lt;Alice&gt;");
            rendered.Html.Should().Be("<p>&lt;Alice&gt;</p>active");
            template!.Fingerprint.Should().NotBeNullOrWhiteSpace();
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task File_templates_fail_for_missing_binding()
    {
        var renderer = new HandlebarsEmailTemplateRenderer();
        var template = new EmailTemplate { Key = "x", Subject = "{{missing}}", Html = "body" };
        var action = () => renderer.RenderAsync(template, new { });
        await action.Should().ThrowAsync<HandlebarsDotNet.Compiler.HandlebarsUndefinedBindingException>();
    }
}
