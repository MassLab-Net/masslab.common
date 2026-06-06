using System.Xml.Linq;

namespace MassLab.Common.Database.Tests;

/// <summary>
/// Tests to verify that each provider package only references its specific database provider
/// and does not have cross-provider dependencies.
/// Validates Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6
/// </summary>
public class PackageDependencyIsolationTests
{
    private const string CommonPath = "MassLab/common";
    
    [Fact]
    public void EFCorePostgreSQL_ShouldNotReference_SqlServerOrMySqlPackages()
    {
        // Arrange
        var projectPath = Path.Combine(GetSolutionRoot(), CommonPath, 
            "MassLab.Common.Database.EFCore.PostgreSQL", 
            "MassLab.Common.Database.EFCore.PostgreSQL.csproj");
        
        // Act
        var packageReferences = GetPackageReferences(projectPath);
        
        // Assert
        packageReferences.Should().NotContain(p => p.Contains("SqlServer"), 
            "EFCore.PostgreSQL should not reference SQL Server packages");
        packageReferences.Should().NotContain(p => p.Contains("MySql"), 
            "EFCore.PostgreSQL should not reference MySQL packages");
        packageReferences.Should().Contain("Npgsql.EntityFrameworkCore.PostgreSQL", 
            "EFCore.PostgreSQL should reference Npgsql");
    }
    
    [Fact]
    public void EFCoreSqlServer_ShouldNotReference_PostgreSqlOrMySqlPackages()
    {
        // Arrange
        var projectPath = Path.Combine(GetSolutionRoot(), CommonPath, 
            "MassLab.Common.Database.EFCore.SqlServer", 
            "MassLab.Common.Database.EFCore.SqlServer.csproj");
        
        // Act
        var packageReferences = GetPackageReferences(projectPath);
        
        // Assert
        packageReferences.Should().NotContain(p => p.Contains("Npgsql"), 
            "EFCore.SqlServer should not reference PostgreSQL packages");
        packageReferences.Should().NotContain(p => p.Contains("MySql"), 
            "EFCore.SqlServer should not reference MySQL packages");
        packageReferences.Should().Contain("Microsoft.EntityFrameworkCore.SqlServer", 
            "EFCore.SqlServer should reference SqlServer");
    }
    
    [Fact]
    public void EFCoreMySQL_ShouldNotReference_PostgreSqlOrSqlServerPackages()
    {
        // Arrange
        var projectPath = Path.Combine(GetSolutionRoot(), CommonPath, 
            "MassLab.Common.Database.EFCore.MySQL", 
            "MassLab.Common.Database.EFCore.MySQL.csproj");
        
        // Act
        var packageReferences = GetPackageReferences(projectPath);
        
        // Assert
        packageReferences.Should().NotContain(p => p.Contains("Npgsql"), 
            "EFCore.MySQL should not reference PostgreSQL packages");
        packageReferences.Should().NotContain(p => p.Contains("SqlServer"), 
            "EFCore.MySQL should not reference SQL Server packages");
        packageReferences.Should().Contain(p => p.Contains("MySql"), 
            "EFCore.MySQL should reference MySQL packages");
    }
    
    [Fact]
    public void DapperPostgreSQL_ShouldNotReference_SqlServerOrMySqlPackages()
    {
        // Arrange
        var projectPath = Path.Combine(GetSolutionRoot(), CommonPath, 
            "MassLab.Common.Database.Dapper.PostgreSQL", 
            "MassLab.Common.Database.Dapper.PostgreSQL.csproj");
        
        // Act
        var packageReferences = GetPackageReferences(projectPath);
        
        // Assert
        packageReferences.Should().NotContain(p => p.Contains("SqlClient"), 
            "Dapper.PostgreSQL should not reference SQL Server packages");
        packageReferences.Should().NotContain(p => p.Contains("MySql"), 
            "Dapper.PostgreSQL should not reference MySQL packages");
        packageReferences.Should().Contain("Npgsql", 
            "Dapper.PostgreSQL should reference Npgsql");
    }
    
    [Fact]
    public void DapperSqlServer_ShouldNotReference_PostgreSqlOrMySqlPackages()
    {
        // Arrange
        var projectPath = Path.Combine(GetSolutionRoot(), CommonPath, 
            "MassLab.Common.Database.Dapper.SqlServer", 
            "MassLab.Common.Database.Dapper.SqlServer.csproj");
        
        // Act
        var packageReferences = GetPackageReferences(projectPath);
        
        // Assert
        packageReferences.Should().NotContain(p => p.Contains("Npgsql"), 
            "Dapper.SqlServer should not reference PostgreSQL packages");
        packageReferences.Should().NotContain(p => p.Contains("MySql"), 
            "Dapper.SqlServer should not reference MySQL packages");
        packageReferences.Should().Contain("Microsoft.Data.SqlClient", 
            "Dapper.SqlServer should reference SqlClient");
    }
    
    [Fact]
    public void DapperMySQL_ShouldNotReference_PostgreSqlOrSqlServerPackages()
    {
        // Arrange
        var projectPath = Path.Combine(GetSolutionRoot(), CommonPath, 
            "MassLab.Common.Database.Dapper.MySQL", 
            "MassLab.Common.Database.Dapper.MySQL.csproj");
        
        // Act
        var packageReferences = GetPackageReferences(projectPath);
        
        // Assert
        packageReferences.Should().NotContain(p => p.Contains("Npgsql"), 
            "Dapper.MySQL should not reference PostgreSQL packages");
        packageReferences.Should().NotContain(p => p.Contains("SqlClient"), 
            "Dapper.MySQL should not reference SQL Server packages");
        packageReferences.Should().Contain("MySqlConnector", 
            "Dapper.MySQL should reference MySqlConnector");
    }
    
    private static List<string> GetPackageReferences(string projectPath)
    {
        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Project file not found: {projectPath}");
        }
        
        var doc = XDocument.Load(projectPath);
        var packageReferences = doc.Descendants("PackageReference")
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
        
        return packageReferences;
    }
    
    private static string GetSolutionRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        
        // Navigate up until we find the MassLab.sln file
        while (!File.Exists(Path.Combine(currentDir, "MassLab", "MassLab.sln")))
        {
            var parent = Directory.GetParent(currentDir);
            if (parent == null)
            {
                throw new InvalidOperationException("Could not find solution root");
            }
            currentDir = parent.FullName;
        }
        
        return currentDir;
    }
}
