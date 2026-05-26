using FinControl.Consolidado.Core.Domain;
using FinControl.Infrastructure.Cache;
using Microsoft.Extensions.Logging;

namespace FinControl.Consolidado.Core.Features.Queries.GetSaldoConsolidado;

public sealed class GetSaldoConsolidadoQueryHandler(
    RedisCacheService cache,
    ILogger<GetSaldoConsolidadoQueryHandler> logger)
{
    private const string CACHE_KEY_ACUMULADO = "saldo:consolidado:acumulado";
    private const int MAX_LOOKBACK_DAYS = 30;

    private static string CacheKey(DateOnly data) => $"saldo:consolidado:{data:yyyy-MM-dd}";

    public async Task<GetSaldoConsolidadoResponse> Handle(
        GetSaldoConsolidadoQuery request,
        CancellationToken cancellationToken)
    {
        if (!request.DataLancamento.HasValue)
            return await GetAcumuladoAsync(cancellationToken);

        return await GetPorDataAsync(request.DataLancamento.Value, cancellationToken);
    }

    private async Task<GetSaldoConsolidadoResponse> GetAcumuladoAsync(CancellationToken ct)
    {
        var saldo = await cache.GetAsync<SaldoConsolidado>(CACHE_KEY_ACUMULADO, ct);
        return new GetSaldoConsolidadoResponse(
            Saldo: saldo?.Saldo ?? 0,
            UltimaAtualizacao: saldo?.UltimaAtualizacao ?? DateTimeOffset.UtcNow);
    }

    private async Task<GetSaldoConsolidadoResponse> GetPorDataAsync(
        DateOnly dataRequisitada,
        CancellationToken ct)
    {
        var keyRequisitada = CacheKey(dataRequisitada);
        var saldo = await cache.GetAsync<SaldoConsolidado>(keyRequisitada, ct);

        if (saldo is null)
            saldo = await BuscarSaldoAnteriorAsync(dataRequisitada, keyRequisitada, ct);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Saldo consolidado retornado | Saldo={Saldo} UltimaAtualizacao={UltimaAtualizacao} CacheKey={CacheKey}",
                saldo.Saldo,
                saldo.UltimaAtualizacao,
                keyRequisitada);

        return new GetSaldoConsolidadoResponse(saldo.Saldo, saldo.UltimaAtualizacao);
    }

    private async Task<SaldoConsolidado> BuscarSaldoAnteriorAsync(
        DateOnly dataRequisitada,
        string keyRequisitada,
        CancellationToken ct)
    {
        for (int i = 1; i <= MAX_LOOKBACK_DAYS; i++)
        {
            var dataAnterior = dataRequisitada.AddDays(-i);
            var saldo = await cache.GetAsync<SaldoConsolidado>(CacheKey(dataAnterior), ct);

            if (saldo is not null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation(
                        "Saldo encontrado em data anterior | DataRequisitada={DataRequisitada} DataEncontrada={DataEncontrada} Tentativa={Tentativa}",
                        dataRequisitada, dataAnterior, i);

                // Propaga para a data requisitada para acelerar consultas futuras ao mesmo dia
                await cache.SetAsync(keyRequisitada, saldo, TimeSpan.FromDays(30), ct);
                return saldo;
            }
        }

        logger.LogWarning(
            "Nenhum saldo encontrado nos últimos {MaxDias} dias anteriores a {Data}. Retornando saldo zero.",
            MAX_LOOKBACK_DAYS, dataRequisitada);

        return new SaldoConsolidado(0, DateTimeOffset.UtcNow);
    }
}
