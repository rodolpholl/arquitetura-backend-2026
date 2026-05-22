using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinControl.Consolidado.Core.Domain;

public record SaldoConsolidado(
    long Saldo,
    DateTimeOffset UltimaAtualizacao
);