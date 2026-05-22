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


        var data = DateOnly.FromDateTime(request.DataLancamento?.ToDateTime(new TimeOnly(0, 0)) ?? DateTimeOffset.UtcNow.UtcDateTime);
        var key = CacheKey(data);


        
        var saldoConsolidado = await cache.GetAsync<SaldoConsolidado>(key, cancellationToken);

        if (saldoConsolidado is null)
            saldoConsolidado = new SaldoConsolidado(0, DateTimeOffset.UtcNow);


        GetSaldoConsolidadoResponse result = new(
            Saldo: saldoConsolidado.Saldo,
            UltimaAtualizacao: saldoConsolidado.UltimaAtualizacao
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
