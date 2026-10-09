using System.Net;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PriceForge.Api;
using PriceForge.Api.Endpoints;
using PriceForge.Api.Middleware;
using PriceForge.Application;
using PriceForge.Infrastructure;
using Serilog;
using Serilog.Events;

DotNetEnv.Env.TraversePath().NoClobber().Load();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.WriteTo.Console(
        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}");
    configuration.WriteTo.Elasticsearch([new Uri(context.Configuration.GetConnectionString("Elasticsearch")!)],
                                        opts =>
                                        {
                                            opts.DataStream = new DataStreamName("logs", "priceforge");
                                            opts.BootstrapMethod = BootstrapMethod.Silent;
                                        });
    configuration.ReadFrom.Configuration(context.Configuration);
    configuration.Enrich.FromLogContext();
    configuration.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
});

builder.Services
       .AddOpenTelemetry()
       .ConfigureResource(resource => resource.AddService("PriceForge.Api"))
       .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation()
                                      .AddHttpClientInstrumentation()
                                      .AddOtlpExporter()
                                      .AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText
                                                                                 = true))
       .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation().AddOtlpExporter());

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPresentation(builder.Configuration);

builder.Services
       .AddHealthChecks()
       .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!, name: "postgres", tags: ["ready"])
       .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis", tags: ["ready"]);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApiDocument(settings =>
{
    settings.Title = "Price Forge API";
    settings.Version = "v1";
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    string[] knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];

    foreach (string proxy in knownProxies)
    {
        options.KnownProxies.Add(IPAddress.Parse(proxy));
    }
});

WebApplication app = builder.Build();

app.UseForwardedHeaders();
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (context, httpContext)
        => context.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
    options.MessageTemplate
        = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms from {ClientIp}";
});
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthCheckEndpoints();
app.MapPriceHubEndpoints();

app.Run();

public partial class Program
{
}
