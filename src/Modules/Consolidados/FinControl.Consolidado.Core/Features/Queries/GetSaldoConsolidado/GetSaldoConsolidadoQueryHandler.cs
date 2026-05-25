using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinControl.Consolidado.Core.Domain;
using FinControl.Infrastructure.Cache;
using Microsoft.Extensions.Logging;

namespace FinControl.Consolidado.Core.Features.Queries.GetSaldoConsolidado;

    public class GetSaldoConsolidadoQueryHandler(
    RedisCacheService cache,
    ILogger<GetSaldoConsolidadoQueryHandler> logger
    )
{
    private static string CacheKey(DateOnly data) => $"saldo:consolidado:{data:yyyy-MM-dd}";

    public async Task<GetSaldoConsolidadoResponse> Handle(GetSaldoConsolidadoQuery request, CancellationToken cancellationToken)
    {

        var dataAtual = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var data = DateOnly.FromDateTime(request.DataLancamento?.ToDateTime(new TimeOnly(0, 0)) ?? DateTimeOffset.UtcNow.UtcDateTime);
        var key = CacheKey(data);


        
        var saldoConsolidado = await cache.GetAsync<SaldoConsolidado>(key, cancellationToken);

        if (saldoConsolidado is null)
        {

            for (int i = 0; i < 30; i++)
            {
                data = data.AddDays(-1);
                key = CacheKey(data);

                if (logger.IsEnabled(LogLevel.Warning))
                    logger.LogWarning(
                        "Saldo consolidado não encontrado no cache. buscando o saldo anterior para inicialização do dia | Data={Data} Tentativa={Tentativa} CacheKey={CacheKey}",
                        data,
                        i + 1,
                        key);

                await Task.Delay(100, cancellationToken);
                saldoConsolidado = await cache.GetAsync<SaldoConsolidado>(key, cancellationToken);

                if (saldoConsolidado is not null)
                {
                    await cache.SetAsync(CacheKey(DateOnly.FromDateTime(request.DataLancamento?.ToDateTime(new TimeOnly(0, 0)) ?? DateTimeOffset.UtcNow.UtcDateTime)), saldoConsolidado, TimeSpan.FromDays(30), cancellationToken);
                    break;
                }
            }
        }
        else
            saldoConsolidado = new SaldoConsolidado(0, DateTimeOffset.UtcNow);
        


        GetSaldoConsolidadoResponse result = new(
            Saldo: saldoConsolidado?.Saldo ?? 0,
            UltimaAtualizacao: saldoConsolidado?.UltimaAtualizacao ?? DateTimeOffset.UtcNow.UtcDateTime
        );

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Saldo consolidado retornado | Saldo={Saldo} UltimaAtualizacao={UltimaAtualizacao} CacheKey={CacheKey}",
                result.SaldoDecimal,
                result.UltimaAtualizacao,
                key);
            
        return result;
    }

}
