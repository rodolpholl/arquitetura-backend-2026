using FinControl.SharedKernel.Messaging;

namespace FinControl.Consolidado.Core.Features.Commands.AtualizarSaldoConsolidao;

// Mantém o nome antigo como alias para não quebrar o Worker até a próxima refatoração de namespace
[Obsolete("Use AtualizarSaldoConsolidadoCommand")]
public record AtualizarSaldoConsolidaoCommand(long ValorLancamento, DateTimeOffset DataLancamento)
    : AtualizarSaldoConsolidadoCommand(ValorLancamento, DataLancamento);

public record AtualizarSaldoConsolidadoCommand(long ValorLancamento, DateTimeOffset DataLancamento) : ICommand;

