using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinControl.Consolidado.Core.Features.Queries.GetSaldoConsolidado;

public record GetSaldoConsolidadoResponse(
    long Saldo,
    DateTimeOffset UltimaAtualizacao
)
{       
    public decimal SaldoDecimal => Saldo / 100m;

}
