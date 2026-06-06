using Microsoft.Extensions.DependencyInjection;
using MassLab.Common.Observability.Configuration;
using MassLab.Common.Observability.Extensions;

namespace MassLab.Common.Observability.Tests;

public class ObservabilityTests
{
    [Fact]
    public void Options_expose_expected_defaults()
    {
        var options = new ObservabilityOptions();

        options.ServiceName.Should().Be("masslab-service");
        options.OtlpEndpoint.Should().Be("http://localhost:4317");
        options.EnablePrometheus.Should().BeTrue();
    }

    [Fact]
    public void Registration_accepts_disabled_tracing_and_metrics()
    {
        var services = new ServiceCollection();

        services.AddMassLabObservability(configureOptions: o =>
        {
            o.EnableTracing = false;
            o.EnableMetrics = false;
        });

        services.Should().NotBeEmpty();
    }

    [Fact]
    public void Registration_rejects_invalid_otlp_endpoint()
    {
        var services = new ServiceCollection();

        var act = () => services.AddMassLabObservability(configureOptions: o =>
        {
            o.OtlpEndpoint = "not-a-uri";
        });

        act.Should().Throw<ArgumentException>()
            .WithParameterName("OtlpEndpoint");
    }

    [Fact]
    public void Registration_rejects_invalid_prometheus_endpoint()
    {
        var services = new ServiceCollection();

        var act = () => services.AddMassLabObservability(configureOptions: o =>
        {
            o.EnableTracing = false;
            o.EnableMetrics = false;
            o.PrometheusEndpoint = "metrics";
        });

        act.Should().Throw<ArgumentException>()
            .WithParameterName("PrometheusEndpoint");
    }
}
