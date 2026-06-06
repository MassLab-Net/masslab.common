namespace MassLab.Common.Swagger.Configuration;

/// <summary>
/// Options for the MassLab Swagger registration helper.
/// </summary>
public class SwaggerOptions
{
    /// <summary>Configuration section name (<c>Swagger</c>).</summary>
    public const string SectionName = "Swagger";

    /// <summary>Document title (<c>info.title</c>).</summary>
    public string Title { get; set; } = "MassLab API";

    /// <summary>Document description (<c>info.description</c>).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Contact name (<c>info.contact.name</c>).</summary>
    public string ContactName { get; set; } = string.Empty;

    /// <summary>Contact email (<c>info.contact.email</c>).</summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>If <c>true</c>, the JWT bearer security scheme is added.</summary>
    public bool EnableJwtBearer { get; set; } = true;

    /// <summary>Path prefix for SwaggerUI (default <c>/swagger</c>).</summary>
    public string RoutePrefix { get; set; } = "swagger";

    /// <summary>OpenAPI document version emitted by Swagger middleware. Defaults to <c>3.0</c>.</summary>
    public string OpenApiVersion { get; set; } = "3.0";
}
