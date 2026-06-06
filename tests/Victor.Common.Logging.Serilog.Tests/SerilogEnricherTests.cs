using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Victor.Common.Logging.Serilog.Extensions;
using Victor.Common.Logging.Serilog.Enrichers;

namespace Victor.Common.Logging.Serilog.Tests;

public class SerilogEnricherTests
{
    [Fact]
    public void Trace_enricher_adds_trace_id_from_http_context()
    {
        var context = new DefaultHttpContext();
        context.Items["TraceId"] = "trace-abc";
        var logEvent = NewEvent();

        new TraceIdEnricher(new HttpContextAccessor { HttpContext = context })
            .Enrich(logEvent, new TestPropertyFactory());

        logEvent.Properties["TraceId"].ToString().Should().Contain("trace-abc");
    }

    [Fact]
    public void Tenant_enricher_adds_tenant_from_items()
    {
        var context = new DefaultHttpContext();
        context.Items["TenantId"] = "tenant-1";
        var logEvent = NewEvent();

        new TenantIdEnricher(new HttpContextAccessor { HttpContext = context })
            .Enrich(logEvent, new TestPropertyFactory());

        logEvent.Properties["TenantId"].ToString().Should().Contain("tenant-1");
    }

    [Fact]
    public void AddSerilogLogging_registers_shared_logger_and_http_context_accessor()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:EnableConsole"] = "false",
                ["Logging:MinimumLevel"] = "Warning"
            })
            .Build();

        services.AddSerilogLogging(configuration);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<global::Serilog.ILogger>().Should().NotBeNull();
        provider.GetRequiredService<IHttpContextAccessor>().Should().NotBeNull();
    }

    private static LogEvent NewEvent()
        => new(DateTimeOffset.UtcNow, LogEventLevel.Information, null,
            new MessageTemplateParser().Parse("test"), []);

    private sealed class TestPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
            => new(name, new ScalarValue(value));
    }
}
