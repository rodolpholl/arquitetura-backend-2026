using FinControl.Consolidado.Core.Domain;
using FinControl.Infrastructure.Cache;
using Microsoft.Extensions.Logging;

namespace FinControl.Consolidado.Core.Features.Commands.AtualizarSaldoConsolidao;

public class AtualizarSaldoConsolidadoCommandHandler(
    RedisCacheService cache,
    IRedisLockService lockService,
    ILogger<AtualizarSaldoConsolidadoCommandHandler> logger)
{
    private static string CacheKey(DateOnly data) => $"saldo:consolidado:{data:yyyy-MM-dd}";
    private static string LockKey(DateOnly data) => $"lock:saldo:consolidado:{data:yyyy-MM-dd}";

    public async Task Handle(
        AtualizarSaldoConsolidadoCommand command,
        CancellationToken cancellationToken = default)
    {
        // Usa a data do lançamento — não a data atual — para consolidar no dia correto
        var data = DateOnly.FromDateTime(command.DataLancamento.UtcDateTime);
        var key = CacheKey(data);

        var acquired = await lockService.ExecuteWithLockAsync(
            lockKey: LockKey(data),
            action: async () =>
            {
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
            },
            lockExpiry: TimeSpan.FromSeconds(10),
            ct: cancellationToken);

        if (!acquired)
            throw new InvalidOperationException(
                $"Nao foi possivel adquirir lock de consolidacao para o dia {data:yyyy-MM-dd}. " +
                "O evento sera reprocessado pelo message broker.");
    }
}

// Alias para compatibilidade — Worker e testes ainda referenciam o nome antigo
#pragma warning disable CS0618
public class AtualizarSaldoConsolidaoCommandHandler(
    RedisCacheService cache,
    IRedisLockService lockService,
    ILogger<AtualizarSaldoConsolidadoCommandHandler> logger)
    : AtualizarSaldoConsolidadoCommandHandler(cache, lockService, logger)
{
    public Task Handle(AtualizarSaldoConsolidaoCommand command, CancellationToken ct = default)
        => base.Handle(command, ct);
}
#pragma warning restore CS0618
