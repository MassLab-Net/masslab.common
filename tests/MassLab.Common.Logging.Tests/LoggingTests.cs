using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MassLab.Common.Logging.Abstractions;
using MassLab.Common.Logging.Configuration;
using MassLab.Common.Logging.Extensions;
using MassLab.Common.Logging.Implementations;

namespace MassLab.Common.Logging.Tests;

public class LoggingTests
{
    [Fact]
    public void Logging_options_have_production_safe_defaults()
    {
        var options = new LoggingOptions();

        options.MinimumLevel.Should().Be("Information");
        options.EnableConsole.Should().BeTrue();
        options.EnableOpenTelemetry.Should().BeFalse();
    }

    [Fact]
    public void Logger_adapter_rejects_null_logger()
    {
        var act = () => new LoggerAdapter<LoggingTests>(null!);

        act.Should().Throw<ArgumentNullException>();
        new LoggerAdapter<LoggingTests>(NullLogger<LoggingTests>.Instance).Should().NotBeNull();
    }

    [Fact]
    public void Logger_adapter_allows_null_scope_from_underlying_logger()
    {
        var adapter = new LoggerAdapter<LoggingTests>(NullLogger<LoggingTests>.Instance);

        using var scope = adapter.BeginScope("scope");

        scope.Should().NotBeNull();
    }

    [Fact]
    public void AddCommonLogging_registers_adapter_once()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddLogging();
        services.AddCommonLogging(configuration);
        services.AddCommonLogging(configuration);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ILoggerAdapter<LoggingTests>>().Should().BeOfType<LoggerAdapter<LoggingTests>>();
        services.Count(d => d.ServiceType == typeof(ILoggerAdapter<>)).Should().Be(1);
    }
}
