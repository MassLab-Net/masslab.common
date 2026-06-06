namespace MassLab.Common.Caching.Exceptions;

/// <summary>
/// Exception thrown when cache configuration is invalid.
/// </summary>
public class CacheConfigurationException : Exception
{
    /// <summary>
    /// Gets the name of the configuration setting that is invalid.
    /// </summary>
    public string SettingName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheConfigurationException"/> class.
    /// </summary>
    /// <param name="settingName">The name of the configuration setting that is invalid.</param>
    /// <param name="message">The error message describing the configuration issue.</param>
    public CacheConfigurationException(string settingName, string message)
        : base($"Invalid cache configuration for setting '{settingName}': {message}")
    {
        SettingName = settingName;
    }
}
