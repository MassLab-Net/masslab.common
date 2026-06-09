# MassLab.Common.Messaging

Use this for integration event abstractions and event handlers. Always choose a provider.

NuGet:

```bash
dotnet add package MassLab.Common.Messaging
```

Setup:

```csharp
builder.Services.AddMassLabMessagingCore();
builder.Services.AddIntegrationEventHandlers(typeof(ProductPriceChangedHandler).Assembly);
```

Main types:

```csharp
IEventBus
IIntegrationEvent
IIntegrationEventHandler<TEvent>
```

Choose provider: `InMemory`, `Kafka`, or `RabbitMQ`.

Read deeper only when needed: `MassLab.Common.Messaging/README.md`.

