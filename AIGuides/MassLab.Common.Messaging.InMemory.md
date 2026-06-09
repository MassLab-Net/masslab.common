# MassLab.Common.Messaging.InMemory

Use this for local development and tests when events only need in-process dispatch.

NuGet:

```bash
dotnet add package MassLab.Common.Messaging.InMemory
```

Setup:

```csharp
builder.Services.AddMassLabMessagingCore();
builder.Services.AddIntegrationEventHandlers(typeof(ProductPriceChangedHandler).Assembly);
builder.Services.AddInMemoryEventBus();
```

Do not use for cross-service production messaging because events are lost when the process exits.

Read deeper only when needed: `MassLab.Common.Messaging.InMemory/README.md`.

