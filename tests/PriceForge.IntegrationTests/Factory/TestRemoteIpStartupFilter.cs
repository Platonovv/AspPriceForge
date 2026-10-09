using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace PriceForge.IntegrationTests.Factory;

public sealed class TestRemoteIpStartupFilter : IStartupFilter
{
    public const string RemoteIpHeader = "X-Test-RemoteIp";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(RemoteIpHeader, out var value)
                    && IPAddress.TryParse(value, out IPAddress? address))
                {
                    context.Connection.RemoteIpAddress = address;
                }

                await nextMiddleware();
            });

            next(app);
        };
}
