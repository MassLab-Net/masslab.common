# MassLab.Common.Database.Dapper.SqlServer

Use this for Dapper repositories backed by SQL Server.

NuGet:

```bash
dotnet add package MassLab.Common.Database.Dapper.SqlServer
```

Typical config section: `Database` with read/write connection strings and `UseSeparateReadDb`.

Use `IDapperReadRepository` for SELECT queries and `IDapperWriteRepository` for commands.

Read deeper only when needed: `MassLab.Common.Database.Dapper.SqlServer/README.md`.

