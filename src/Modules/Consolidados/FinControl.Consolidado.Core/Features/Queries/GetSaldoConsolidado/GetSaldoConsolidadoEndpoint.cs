using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Wolverine.Http;

namespace FinControl.Consolidado.Core.Features.Queries.GetSaldoConsolidado;

public class GetSaldoConsolidadoEndpoint
{
    [Authorize]
    [WolverineGet("/consolidados/saldo")]
    public static async Task<GetSaldoConsolidadoResponse> Handle(
        [FromQuery(Name = "data-lancamento")] DateOnly? dataLancamento,
        IMessageBus bus,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSaldoConsolidadoQuery(dataLancamento);
        return await bus.InvokeAsync<GetSaldoConsolidadoResponse>(query, cancellationToken);
    }
}
