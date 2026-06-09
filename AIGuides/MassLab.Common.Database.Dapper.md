# MassLab.Common.Database.Dapper

Use this for high-performance raw SQL repositories with Dapper.

NuGet:

```bash
dotnet add package MassLab.Common.Database.Dapper
```

Main abstractions:

```csharp
IDapperReadRepository
IDapperWriteRepository
```

Use provider-specific packages for connection factories:

- `MassLab.Common.Database.Dapper.PostgreSQL`
- `MassLab.Common.Database.Dapper.SqlServer`
- `MassLab.Common.Database.Dapper.MySQL`

Read deeper only when needed: `MassLab.Common.Database.Dapper/README.md`.

