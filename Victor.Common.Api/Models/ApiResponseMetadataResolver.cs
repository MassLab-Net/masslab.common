using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Victor.Common.Api.Models;

internal static class ApiResponseMetadataResolver
{
    private static readonly string DefaultVersion = ResolveAssemblyVersion();

    internal static string ResolveTraceId(HttpContext? context)
    {
        if (context?.Items.TryGetValue("TraceId", out var traceId) == true && traceId is not null)
            return traceId.ToString() ?? string.Empty;

        if (Activity.Current?.Id != null)
            return Activity.Current.Id;

        return context?.TraceIdentifier ?? string.Empty;
    }

    internal static string ResolveApiVersion(HttpContext? context)
    {
        var routeVersion = context?.Request.RouteValues["version"]?.ToString();
        if (!string.IsNullOrWhiteSpace(routeVersion))
            return NormalizeVersion(routeVersion);

        if (context?.Request.Headers.TryGetValue("X-Api-Version", out var headerVersion) == true &&
            !string.IsNullOrWhiteSpace(headerVersion))
            return NormalizeVersion(headerVersion.ToString());

        var queryVersion = context?.Request.Query["api-version"].ToString();
        if (!string.IsNullOrWhiteSpace(queryVersion))
            return NormalizeVersion(queryVersion);

        return DefaultVersion;
    }

    private static string NormalizeVersion(string version) =>
        version.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? version[1..]
            : version;

    private static string ResolveAssemblyVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
    }
}
