# MassLab Common Guide

Dung file nay truoc khi them code vao du an MassLab. Cac common package da duoc publish len NuGet, vi vay khi can dung common nao thi uu tien cai package tu NuGet thay vi copy source trong repo nay.

Quy tac cho AI:

1. Xac dinh nhu cau cua service.
2. Chon package trong bang ben duoi.
3. Doc file guide rieng cua package trong `AIGuides`.
4. Cai bang `dotnet add package <PackageId>`.
5. Chi tham khao README/source cua package tuong ung khi can chi tiet them, khong copy code common vao service.

## Quick Map

| Khi nguoi dung noi | Dung package | Doc guide |
| --- | --- | --- |
| API response, exception middleware, trace id, CORS, health checks | `MassLab.Common.Api` | `MassLab.Common.Api.md` |
| API versioning | `MassLab.Common.ApiVersioning` | `MassLab.Common.ApiVersioning.md` |
| JWT, API key, current user, token service | `MassLab.Common.Authentication` | `MassLab.Common.Authentication.md` |
| Permission/scope authorization | `MassLab.Common.Authorization` | `MassLab.Common.Authorization.md` |
| Background job abstraction | `MassLab.Common.BackgroundJobs` | `MassLab.Common.BackgroundJobs.md` |
| In-process background jobs | `MassLab.Common.BackgroundJobs.Hosted` | `MassLab.Common.BackgroundJobs.Hosted.md` |
| Hangfire jobs/dashboard | `MassLab.Common.BackgroundJobs.Hangfire` | `MassLab.Common.BackgroundJobs.Hangfire.md` |
| Quartz scheduled jobs | `MassLab.Common.BackgroundJobs.Quartz` | `MassLab.Common.BackgroundJobs.Quartz.md` |
| Cache abstraction | `MassLab.Common.Caching` | `MassLab.Common.Caching.md` |
| Memory cache | `MassLab.Common.Caching.Memory` | `MassLab.Common.Caching.Memory.md` |
| Redis cache, distributed lock | `MassLab.Common.Caching.Redis` | `MassLab.Common.Caching.Redis.md` |
| Repository/unit of work abstraction | `MassLab.Common.Database` | `MassLab.Common.Database.md` |
| EF Core repositories | `MassLab.Common.Database.EFCore` | `MassLab.Common.Database.EFCore.md` |
| EF Core PostgreSQL | `MassLab.Common.Database.EFCore.PostgreSQL` | `MassLab.Common.Database.EFCore.PostgreSQL.md` |
| EF Core SQL Server | `MassLab.Common.Database.EFCore.SqlServer` | `MassLab.Common.Database.EFCore.SqlServer.md` |
| EF Core MySQL | `MassLab.Common.Database.EFCore.MySQL` | `MassLab.Common.Database.EFCore.MySQL.md` |
| Dapper repositories | `MassLab.Common.Database.Dapper` | `MassLab.Common.Database.Dapper.md` |
| Dapper PostgreSQL | `MassLab.Common.Database.Dapper.PostgreSQL` | `MassLab.Common.Database.Dapper.PostgreSQL.md` |
| Dapper SQL Server | `MassLab.Common.Database.Dapper.SqlServer` | `MassLab.Common.Database.Dapper.SqlServer.md` |
| Dapper MySQL | `MassLab.Common.Database.Dapper.MySQL` | `MassLab.Common.Database.Dapper.MySQL.md` |
| Domain primitives, entity, aggregate, value object, specification | `MassLab.Common.Domain` | `MassLab.Common.Domain.md` |
| gRPC helpers/interceptors/health/reflection | `MassLab.Common.Grpc` | `MassLab.Common.Grpc.md` |
| HttpClient handlers/policies | `MassLab.Common.HttpClient` | `MassLab.Common.HttpClient.md` |
| Idempotency-Key middleware | `MassLab.Common.Idempotency` | `MassLab.Common.Idempotency.md` |
| Logging abstraction | `MassLab.Common.Logging` | `MassLab.Common.Logging.md` |
| Serilog integration | `MassLab.Common.Logging.Serilog` | `MassLab.Common.Logging.Serilog.md` |
| Messaging abstraction/events/handlers | `MassLab.Common.Messaging` | `MassLab.Common.Messaging.md` |
| In-memory event bus | `MassLab.Common.Messaging.InMemory` | `MassLab.Common.Messaging.InMemory.md` |
| Kafka event bus | `MassLab.Common.Messaging.Kafka` | `MassLab.Common.Messaging.Kafka.md` |
| RabbitMQ event bus | `MassLab.Common.Messaging.RabbitMQ` | `MassLab.Common.Messaging.RabbitMQ.md` |
| Multitenancy | `MassLab.Common.Multitenancy` | `MassLab.Common.Multitenancy.md` |
| OpenTelemetry, Prometheus | `MassLab.Common.Observability` | `MassLab.Common.Observability.md` |
| Transactional outbox | `MassLab.Common.Outbox` | `MassLab.Common.Outbox.md` |
| Rate limiting | `MassLab.Common.RateLimiting` | `MassLab.Common.RateLimiting.md` |
| Blob storage abstraction | `MassLab.Common.Storage` | `MassLab.Common.Storage.md` |
| Azure Blob Storage | `MassLab.Common.Storage.AzureBlob` | `MassLab.Common.Storage.AzureBlob.md` |
| File-system storage | `MassLab.Common.Storage.FileSystem` | `MassLab.Common.Storage.FileSystem.md` |
| S3/MinIO storage | `MassLab.Common.Storage.S3` | `MassLab.Common.Storage.S3.md` |
| Swagger/OpenAPI | `MassLab.Common.Swagger` | `MassLab.Common.Swagger.md` |
| FluentValidation + MediatR validation pipeline | `MassLab.Common.Validation` | `MassLab.Common.Validation.md` |

## Provider Rules

- Cache: them `MassLab.Common.Caching` cho abstraction va chon mot provider `Memory` hoac `Redis`.
- Messaging: them `MassLab.Common.Messaging` cho abstraction va chon mot provider `InMemory`, `Kafka`, hoac `RabbitMQ`.
- Storage: them `MassLab.Common.Storage` cho abstraction va chon mot provider `FileSystem`, `S3`, hoac `AzureBlob`.
- Database: them `MassLab.Common.Database` cho abstraction, chon EFCore hoac Dapper, roi chon provider database.
- Background jobs: them `MassLab.Common.BackgroundJobs` cho abstraction va chon `Hosted`, `Hangfire`, hoac `Quartz`.

## NuGet Usage

```bash
dotnet add package MassLab.Common.Grpc
```

Khong pin version tru khi du an yeu cau. De NuGet resolve version moi nhat dang co trong feed.

