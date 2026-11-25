using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Vagalume.Infra.Observability;

namespace Vagalume.Api.Observability;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddVagalumeObservability(this WebApplicationBuilder builder)
    {
        var serviceName = builder.Configuration["OTEL_SERVICE_NAME"]
            ?? builder.Configuration["OpenTelemetry:ServiceName"]
            ?? VagalumeTelemetry.ServiceName;

        var serviceVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "1.0.0";

        if (!builder.Environment.IsDevelopment())
        {
            builder.Logging.AddJsonConsole(opts =>
            {
                opts.IncludeScopes = true;
                opts.UseUtcTimestamp = true;
                opts.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
            });
        }

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
        });

        var otel = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment.name"] = builder.Environment.EnvironmentName
                }))
            .WithTracing(tracing => tracing
                .AddSource(VagalumeTelemetry.ActivitySourceName)
                .AddAspNetCoreInstrumentation(opts =>
                {
                    opts.RecordException = true;
                    opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation(opts =>
                {
                    opts.RecordException = true;
                    opts.EnrichWithHttpRequestMessage = (activity, request) =>
                    {
                        var uri = request.RequestUri;
                        if (uri is null)
                            return;

                        var host = uri.Host;
                        var path = uri.AbsolutePath;
                        var ehWebhookSensivel = host.Contains("discord", StringComparison.OrdinalIgnoreCase)
                            || path.Contains("/webhooks/", StringComparison.OrdinalIgnoreCase)
                            || path.Contains("/hook", StringComparison.OrdinalIgnoreCase);

                        if (ehWebhookSensivel)
                        {
                            activity.SetTag("url.full", $"{uri.Scheme}://{uri.Host}/[REDACTED]");
                            activity.SetTag("url.path", "/[REDACTED]");
                            activity.SetTag("url.query", null);
                        }
                    };
                })
                .AddNpgsql())
            .WithMetrics(metrics => metrics
                .AddMeter(VagalumeTelemetry.MeterName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddNpgsqlInstrumentation());

        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? builder.Configuration["OpenTelemetry:OtlpEndpoint"];

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            otel.UseOtlpExporter();
        }

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

        return builder;
    }

    public static WebApplication MapVagalumeHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        return app;
    }
}
