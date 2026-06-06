using System.Xml.Linq;

namespace Victor.Common.Database.Tests;

public class ProviderIsolationPropertyTests
{
    private static readonly string[] ProviderProjects =
    [
        "Victor.Common.Database.EFCore.PostgreSQL",
        "Victor.Common.Database.EFCore.SqlServer",
        "Victor.Common.Database.EFCore.MySQL",
        "Victor.Common.Database.Dapper.PostgreSQL",
        "Victor.Common.Database.Dapper.SqlServer",
        "Victor.Common.Database.Dapper.MySQL"
    ];

    [Property(MaxTest = 100)]
    public bool Provider_packages_do_not_mix_database_drivers(FsCheck.NonNegativeInt index)
    {
        // Feature: database-provider-separation, Property: provider packages isolate database-driver dependencies.
        var project = ProviderProjects[index.Get % ProviderProjects.Length];
        var references = GetPackageReferences(ProjectPath(project));

        var driverFamilies = new[]
        {
            references.Any(r => r.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)),
            references.Any(r => r.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) || r.Contains("SqlServer", StringComparison.OrdinalIgnoreCase)),
            references.Any(r => r.Contains("MySql", StringComparison.OrdinalIgnoreCase))
        };

        return driverFamilies.Count(BooleanIsTrue) == 1;
    }

    private static bool BooleanIsTrue(bool value) => value;

    private static string ProjectPath(string project)
        => Path.Combine(GetSolutionRoot(), "Victor", "common", project, $"{project}.csproj");

    private static List<string> GetPackageReferences(string projectPath)
        => XDocument.Load(projectPath)
            .Descendants("PackageReference")
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

    private static string GetSolutionRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(currentDir, "Victor", "Victor.sln")))
            currentDir = Directory.GetParent(currentDir)?.FullName
                         ?? throw new InvalidOperationException("Could not find solution root");
        return currentDir;
    }
}
