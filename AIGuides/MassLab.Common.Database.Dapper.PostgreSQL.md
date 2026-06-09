# MassLab.Common.Database.Dapper.PostgreSQL

Use this for Dapper repositories backed by PostgreSQL.

NuGet:

```bash
dotnet add package MassLab.Common.Database.Dapper.PostgreSQL
```

Typical config section: `Database` with read/write connection strings and `UseSeparateReadDb`.

Use `IDapperReadRepository` for SELECT queries and `IDapperWriteRepository` for commands.

Read deeper only when needed: `MassLab.Common.Database.Dapper.PostgreSQL/README.md`.

