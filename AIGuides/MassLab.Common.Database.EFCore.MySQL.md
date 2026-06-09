# MassLab.Common.Database.EFCore.MySQL

Use this for EF Core repositories backed by MySQL.

NuGet:

```bash
dotnet add package MassLab.Common.Database.EFCore.MySQL
```

Typical config section: `Database` with `WriteConnectionString`, optional `ReadConnectionString`, and `UseSeparateReadDb`.

The provider detects server version through MySQL connection settings.

Read deeper only when needed: `MassLab.Common.Database.EFCore.MySQL/README.md`.

