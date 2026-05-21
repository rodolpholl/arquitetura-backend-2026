using FinControl.Infrastructure.Vault;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

namespace FinControl.Infrastructure.Extensions;

/// <summary>
/// Configura Serilog com:
///   - Console (texto legivel em dev, JSON em prod)
///   - File (rotacao diaria)
///   - Grafana Loki (push de logs estruturados)
/// Enriquece com: CorrelationId, MachineName, ThreadId, TraceId via OpenTelemetry.
/// </summary>
public static class SerilogExtensions
{
    public static WebApplicationBuilder AddFinControlSerilog(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        builder.Host.UseSerilog((ctx, services, config) =>
        {
            // VaultKeys.LokiUrl → "grafana:loki_url" (Vault path: dev/grafana → loki_url)
            var lokiUrl  = ctx.Configuration[VaultKeys.LokiUrl]
                           ?? throw new InvalidOperationException(
                               $"Secret '{VaultKeys.LokiUrl}' não encontrado no Vault (dev/grafana → loki_url). " +
                               "Configure o Vault antes de inicializar o Serilog.");
            var ambiente = ctx.HostingEnvironment.EnvironmentName;

            config
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft",                     LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http",               LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Service", serviceName)
                .Enrich.WithProperty("Ambiente", ambiente)
                // Console: JSON compacto em producao, texto legivel em dev
                .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter())
                // File: rotacao diaria, mantém 7 dias
                .WriteTo.File(
                    path: $"logs/{serviceName}-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    formatter: new Serilog.Formatting.Compact.CompactJsonFormatter())
                // Grafana Loki: push de logs estruturados
                .WriteTo.GrafanaLoki(
                    lokiUrl,
                    labels:
                    [
                        new LokiLabel { Key = "service",  Value = serviceName },
                        new LokiLabel { Key = "ambiente", Value = ambiente }
                    ]);
        });

        return builder;
    }

    /// <summary>
    /// Adiciona middleware de request logging do Serilog.
    /// Chamar ANTES dos endpoints: app.UseFinControlRequestLogging()
    /// </summary>
    public static IApplicationBuilder UseFinControlRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(opts =>
        {
            opts.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} respondido {StatusCode} em {Elapsed:0.0000}ms | CorrelationId: {CorrelationId}";

            opts.EnrichDiagnosticContext = (diag, httpContext) =>
            {
                diag.Set("CorrelationId", httpContext.Items["X-Correlation-Id"]);
                diag.Set("UserAgent",     httpContext.Request.Headers.UserAgent.ToString());
                diag.Set("RemoteIp",      httpContext.Connection.RemoteIpAddress?.ToString());
            };

            // Ignorar health checks do log de request
            opts.GetLevel = (ctx, elapsed, ex) =>
                ctx.Request.Path.StartsWithSegments("/health")
                    ? LogEventLevel.Verbose
                    : LogEventLevel.Information;
        });
    }
}
