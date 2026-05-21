using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FinControl.Infrastructure.Wolverine;

/// <summary>
/// Middleware Wolverine 5.x de logging por convencao (Before/After/Finally).
/// Wolverine 5.x nao usa mais HandlerContinuation — para cancelar, lance uma excecao.
/// </summary>
public sealed class LoggingMiddleware
{
    /// <summary>Executado antes do handler.</summary>
    public static void Before<T>(
        T message,
        ILogger<LoggingMiddleware> logger,
        Stopwatch stopwatch) where T : class
    {
        stopwatch.Restart();
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Wolverine iniciando {MessageType}", typeof(T).Name);
    }

    /// <summary>Executado apos o handler concluir com sucesso.</summary>
    public static void After<T>(
        T message,
        ILogger<LoggingMiddleware> logger,
        Stopwatch stopwatch) where T : class
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Wolverine {MessageType} concluido em {ElapsedMs}ms",
                typeof(T).Name,
                stopwatch.ElapsedMilliseconds);
    }

    /// <summary>Executado sempre — inclusive em caso de excecao.</summary>
    public static void Finally<T>(
        T message,
        ILogger<LoggingMiddleware> logger,
        Exception? exception,
        Stopwatch stopwatch) where T : class
    {
        if (exception is not null)
            logger.LogError(
                exception,
                "Wolverine {MessageType} falhou em {ElapsedMs}ms",
                typeof(T).Name,
                stopwatch.ElapsedMilliseconds);
    }
}
