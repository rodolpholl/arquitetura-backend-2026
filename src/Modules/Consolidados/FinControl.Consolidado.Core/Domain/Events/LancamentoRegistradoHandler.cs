using FinControl.Consolidado.Core.Features.Commands.AtualizarSaldoConsolidao;
using FinControl.SharedKernel.Domain.Events;
using Wolverine;

namespace FinControl.Consolidado.Core.Domain.Events;

public class LancamentoRegistradoHandler(IMessageBus bus)
{
    public Task Handle(LancamentoRegistradoMessage message, CancellationToken ct)
        => bus.InvokeAsync(new AtualizarSaldoConsolidaoCommand(message.Valor), ct);
}
