using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using System.Net.Mail;
using System.Reflection;
using MassLab.Common.Swagger.Configuration;

namespace MassLab.Common.Swagger.Extensions;

/// <summary>
/// Service-collection &amp; application-builder extensions for Swagger.
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Registers Swashbuckle with optional JWT-bearer security scheme, XML
    /// comments, and one OpenAPI document per registered API version (when
    /// <c>AddMassLabApiVersioning()</c> has been called).
    /// </summary>
    public static IServiceCollection AddSwaggerWithJwt(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = SwaggerOptions.SectionName,
        params Assembly[] xmlAssemblies)
    {
        var swaggerOpts = new SwaggerOptions();
        if (configuration != null)
        {
            configuration.GetSection(sectionName).Bind(swaggerOpts);
            Validate(swaggerOpts);
            services.Configure<SwaggerOptions>(configuration.GetSection(sectionName));
        }
        else
        {
            Validate(swaggerOpts);
            services.Configure<SwaggerOptions>(_ => { });
        }

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(o =>
        {
            // include XML docs (caller assemblies + entry assembly)
            var docAssemblies = xmlAssemblies
                .Append(Assembly.GetEntryAssembly())
                .Append(Assembly.GetCallingAssembly())
                .Where(a => a is not null)
                .Distinct();
            foreach (var asm in docAssemblies)
            {
                if (asm is null) continue;
                var xml = Path.Combine(AppContext.BaseDirectory, $"{asm.GetName().Name}.xml");
                if (File.Exists(xml)) o.IncludeXmlComments(xml, includeControllerXmlComments: true);
            }

            // JWT bearer button (only if enabled)
            if (swaggerOpts.EnableJwtBearer)
            {
                o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "JWT bearer token. Example: \"Bearer {token}\"",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                });
                o.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer", document, null),
                        []
                    }
                });
            }

            // Default document — provider-driven docs are added by AddPerVersionDocs() below
            // when ApiVersioning is also installed.
        });

        // After all registrations, ask the DI container if an
        // IApiVersionDescriptionProvider exists; if so, append per-version docs
        // through a deferred PostConfigure on SwaggerGenOptions.
        services.AddOptions<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>()
            .Configure<IServiceProvider, IOptions<SwaggerOptions>>((opts, sp, masslabOpts) =>
            {
                var provider = sp.GetService<IApiVersionDescriptionProvider>();
                var so = masslabOpts.Value;
                if (provider is null)
                {
                    // single document fallback
                    if (!opts.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey("v1"))
                    {
                        opts.SwaggerDoc("v1", new OpenApiInfo
                        {
                            Title = so.Title,
                            Version = "v1",
                            Description = so.Description,
                        });
                    }
                    return;
                }

                foreach (var d in provider.ApiVersionDescriptions)
                {
                    if (opts.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey(d.GroupName)) continue;
                    opts.SwaggerDoc(d.GroupName, new OpenApiInfo
                    {
                        Title = so.Title,
                        Version = d.ApiVersion.ToString(),
                        Description = d.IsDeprecated ? $"{so.Description} (deprecated)" : so.Description,
                        Contact = string.IsNullOrWhiteSpace(so.ContactEmail)
                            ? null
                            : new OpenApiContact { Name = so.ContactName, Email = so.ContactEmail },
                    });
                }
            });

        return services;
    }

    /// <summary>
    /// Mounts Swagger middleware and SwaggerUI, generating one tab per API
    /// version (requires <c>AddMassLabApiVersioning()</c>).
    /// </summary>
    public static IApplicationBuilder UseSwaggerWithUI(this IApplicationBuilder app)
    {
        var provider = app.ApplicationServices.GetService<IApiVersionDescriptionProvider>();
        var options  = app.ApplicationServices.GetRequiredService<IOptions<SwaggerOptions>>().Value;
        Validate(options);

        app.UseSwagger(o => o.OpenApiVersion = ResolveOpenApiSpecVersion(options.OpenApiVersion));
        app.UseSwaggerUI(o =>
        {
            o.RoutePrefix = options.RoutePrefix;
            if (provider is null)
            {
                o.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
            }
            else
            {
                foreach (var d in provider.ApiVersionDescriptions)
                    o.SwaggerEndpoint($"/swagger/{d.GroupName}/swagger.json", d.GroupName.ToUpperInvariant());
            }
        });
        return app;
    }

    private static OpenApiSpecVersion ResolveOpenApiSpecVersion(string version)
        => version.Trim() switch
        {
            "2.0" => OpenApiSpecVersion.OpenApi2_0,
            "3.0" => OpenApiSpecVersion.OpenApi3_0,
            "3.1" => OpenApiSpecVersion.OpenApi3_1,
            _ => throw new ArgumentException("OpenAPI version must be '2.0', '3.0', or '3.1'.", nameof(version))
        };

    private static void Validate(SwaggerOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Title))
            throw new ArgumentException("Swagger title is required.", nameof(options.Title));
        if (options.RoutePrefix is null)
            throw new ArgumentException("Swagger route prefix cannot be null.", nameof(options.RoutePrefix));
        if (options.RoutePrefix.StartsWith('/'))
            throw new ArgumentException("Swagger route prefix must not start with '/'.", nameof(options.RoutePrefix));
        if (!string.IsNullOrWhiteSpace(options.ContactEmail) && !IsValidEmail(options.ContactEmail))
            throw new ArgumentException("Swagger contact email is invalid.", nameof(options.ContactEmail));
        if (string.IsNullOrWhiteSpace(options.OpenApiVersion))
            throw new ArgumentException("OpenAPI version is required.", nameof(options.OpenApiVersion));

        _ = ResolveOpenApiSpecVersion(options.OpenApiVersion);
    }

    private static bool IsValidEmail(string value)
        => MailAddress.TryCreate(value, out _);
}
