namespace MassLab.Common.Api.Configuration;

/// <summary>
/// CORS configuration options.
/// </summary>
public class CorsOptions
{
    /// <summary>
    /// Gets or sets the allowed origins.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether credentials are allowed.
    /// </summary>
    public bool AllowCredentials { get; set; }

    /// <summary>
    /// Gets or sets the allowed HTTP methods.
    /// </summary>
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE"];

    /// <summary>
    /// Gets or sets the allowed headers.
    /// </summary>
    public string[] AllowedHeaders { get; set; } = ["*"];
}
