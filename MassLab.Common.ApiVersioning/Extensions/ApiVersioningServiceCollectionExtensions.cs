using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace MassLab.Common.ApiVersioning.Extensions;

/// <summary>
/// Extension methods to register API versioning with sensible defaults
/// (URL-segment, header, query-string readers) and an
/// <see cref="IApiVersionDescriptionProvider"/> for Swagger.
/// </summary>
public static class ApiVersioningServiceCollectionExtensions
{
    /// <summary>
    /// Registers API versioning with default version 1.0 and reports versions
    /// in response headers.
    /// </summary>
    public static IServiceCollection AddMassLabApiVersioning(
        this IServiceCollection services,
        Action<ApiVersioningOptions>? configure = null)
    {
        services
            .AddApiVersioning(o =>
            {
                o.DefaultApiVersion = new ApiVersion(1, 0);
                o.AssumeDefaultVersionWhenUnspecified = false;
                o.ReportApiVersions = true;
                o.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("X-Api-Version"),
                    new QueryStringApiVersionReader("api-version"));
                configure?.Invoke(o);
            })
            .AddApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'VVV";
                o.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
