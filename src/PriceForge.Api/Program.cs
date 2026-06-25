using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
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

DotNetEnv.Env.TraversePath().Load();

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

builder.Services.AddApplication();
builder.Services.AddControllers();

WebApplication app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
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
