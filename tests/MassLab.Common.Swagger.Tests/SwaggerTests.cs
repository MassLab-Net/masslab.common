using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using MassLab.Common.Swagger.Configuration;
using MassLab.Common.Swagger.Extensions;

namespace MassLab.Common.Swagger.Tests;

public class SwaggerTests
{
    [Fact]
    public void Default_options_emit_openapi_3_0()
    {
        var options = new SwaggerOptions();

        options.OpenApiVersion.Should().Be("3.0");
    }

    [Fact]
    public void AddSwaggerWithJwt_rejects_invalid_openapi_version()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Swagger:OpenApiVersion"] = "3.2"
            })
            .Build();

        var act = () => services.AddSwaggerWithJwt(configuration);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("version");
    }

    [Fact]
    public void AddSwaggerWithJwt_rejects_invalid_route_prefix()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Swagger:RoutePrefix"] = "/swagger"
            })
            .Build();

        var act = () => services.AddSwaggerWithJwt(configuration);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(SwaggerOptions.RoutePrefix));
    }

    [Fact]
    public void AddSwaggerWithJwt_registers_default_v1_document()
    {
        var services = new ServiceCollection();

        services.AddSwaggerWithJwt();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SwaggerGenOptions>>().Value;

        options.SwaggerGeneratorOptions.SwaggerDocs.Should().ContainKey("v1");
    }
}
