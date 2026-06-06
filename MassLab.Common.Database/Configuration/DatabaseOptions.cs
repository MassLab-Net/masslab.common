namespace MassLab.Common.Database.Configuration;

/// <summary>
/// Configuration options for database connections.
/// Supports separate read and write database configurations.
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Gets or sets the connection string for write operations.
    /// </summary>
    public string WriteConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection string for read operations.
    /// </summary>
    public string ReadConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to use a separate read database.
    /// If false, read operations will use the WriteConnectionString.
    /// </summary>
    public bool UseSeparateReadDb { get; set; }

    /// <summary>
    /// Gets the appropriate connection string for read operations.
    /// Returns ReadConnectionString if UseSeparateReadDb is true; otherwise, returns WriteConnectionString.
    /// </summary>
    /// <returns>The connection string to use for read operations.</returns>
    public string GetReadConnectionString() => UseSeparateReadDb ? ReadConnectionString : WriteConnectionString;
}
