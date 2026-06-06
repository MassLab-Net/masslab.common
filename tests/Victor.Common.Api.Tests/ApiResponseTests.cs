using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Victor.Common.Api.Configuration;
using Victor.Common.Api.Middleware;
using Victor.Common.Api.Models;

namespace Victor.Common.Api.Tests;

public class ApiResponseTests
{
    [Fact]
    public void Result_success_and_failure_preserve_state()
    {
        Result.Success().IsSuccess.Should().BeTrue();
        Result.Failure("bad").Error.Should().Be("bad");
        Result<int>.Success(42).Value.Should().Be(42);
    }

    [Fact]
    public void Factory_uses_trace_id_from_http_context_items()
    {
        var context = new DefaultHttpContext();
        context.Items["TraceId"] = "trace-123";
        var factory = new BaseApiResponseFactory(new HttpContextAccessor { HttpContext = context });

        var response = factory.Failure("invalid", "VALIDATION_ERROR",
            new Dictionary<string, string[]> { ["Name"] = ["required"] });

        response.TraceId.Should().Be("trace-123");
        response.Error!.Fields.Should().ContainKey("Name");
    }

    [Fact]
    public void Factory_uses_requested_api_version_from_route_values()
    {
        var context = new DefaultHttpContext();
        context.Request.RouteValues["version"] = "2.0";
        var factory = new BaseApiResponseFactory(new HttpContextAccessor { HttpContext = context });

        var response = factory.Success();

        response.Version.Should().Be("2.0");
    }

    [Fact]
    public void Factory_uses_requested_api_version_from_header_when_route_value_is_missing()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Version"] = "3.0";
        var factory = new BaseApiResponseFactory(new HttpContextAccessor { HttpContext = context });

        var response = factory.Success();

        response.Version.Should().Be("3.0");
    }

    [Fact]
    public async Task Global_exception_middleware_uses_problem_details_content_type()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new InvalidOperationException("bad"),
            NullLogger<GlobalExceptionMiddleware>.Instance,
            new TestHostEnvironment { EnvironmentName = Environments.Production },
            Options.Create(new ApiResponseOptions { Format = ApiResponseFormat.ProblemDetails }));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().StartWith("application/problem+json");
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void Paged_response_rejects_invalid_pagination_state(int pageNumber, int pageSize)
    {
        var act = () => PagedResponse<int>.Create([1], 1, pageNumber, pageSize);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
