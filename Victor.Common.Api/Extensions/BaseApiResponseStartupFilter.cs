using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Victor.Common.Api.Models;

namespace Victor.Common.Api.Extensions;

/// <summary>
/// Resolves <see cref="BaseApiResponseFactory"/> at startup so the ambient
/// resolver used by the obsolete static <see cref="BaseApiResponse"/> helpers
/// is wired before any request is served.
/// </summary>
internal sealed class BaseApiResponseStartupFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        // Force resolution exactly once. Side-effect: BaseApiResponseFactory ctor
        // calls AmbientResolver.Attach(IHttpContextAccessor).
        _ = app.ApplicationServices.GetService<BaseApiResponseFactory>();
        next(app);
    };
}
