# MassLab.Common.Database.EFCore.PostgreSQL

Use this for EF Core repositories backed by PostgreSQL.

NuGet:

```bash
dotnet add package MassLab.Common.Database.EFCore.PostgreSQL
```

Typical config section: `Database` with `WriteConnectionString`, optional `ReadConnectionString`, and `UseSeparateReadDb`.

Use this package when the service uses PostgreSQL with EF Core. Do not mix it with SQL Server/MySQL provider packages unless the service explicitly has multiple databases.

Read deeper only when needed: `MassLab.Common.Database.EFCore.PostgreSQL/README.md`.

