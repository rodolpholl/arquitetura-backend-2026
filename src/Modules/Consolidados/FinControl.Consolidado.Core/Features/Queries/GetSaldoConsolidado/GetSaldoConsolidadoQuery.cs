using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinControl.SharedKernel.Messaging;

namespace FinControl.Consolidado.Core.Features.Queries.GetSaldoConsolidado;

public record GetSaldoConsolidadoQuery(DateOnly? DataLancamento = null) : IQuery<GetSaldoConsolidadoResponse>;
