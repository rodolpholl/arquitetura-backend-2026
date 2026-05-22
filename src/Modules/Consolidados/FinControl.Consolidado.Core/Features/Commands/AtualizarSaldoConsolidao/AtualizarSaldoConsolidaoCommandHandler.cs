using FinControl.Consolidado.Core.Domain;
using FinControl.Infrastructure.Cache;
using Microsoft.Extensions.Logging;

namespace FinControl.Consolidado.Core.Features.Commands.AtualizarSaldoConsolidao;

public class AtualizarSaldoConsolidaoCommandHandler(
    RedisCacheService cache,
    ILogger<AtualizarSaldoConsolidaoCommandHandler> logger)
{
    // Chave por dia: "saldo:consolidado:2026-05-22"
    private static string CacheKey(DateOnly data) => $"saldo:consolidado:{data:yyyy-MM-dd}";

    public async Task Handle(
        AtualizarSaldoConsolidaoCommand command,
        CancellationToken cancellationToken = default)
    {
        var data = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        var key = CacheKey(data);

        var atual = await cache.GetAsync<SaldoConsolidado>(key, cancellationToken);

        var saldoAnterior = atual?.Saldo ?? 0;
        var novoSaldo = new SaldoConsolidado(
            Saldo: saldoAnterior + command.ValorLancamento,
            UltimaAtualizacao: DateTimeOffset.UtcNow);

        await cache.SetAsync(key, novoSaldo, TimeSpan.FromDays(30), cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Saldo consolidado atualizado | Data={Data} SaldoAnterior={SaldoAnterior} Incremento={Incremento} NovoSaldo={NovoSaldo} CacheKey={CacheKey}",
                data.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                saldoAnterior,
                command.ValorLancamento,
                novoSaldo.Saldo,
                key);
    }
}
