using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinControl.SharedKernel.Messaging;

namespace FinControl.Consolidado.Core.Features.Commands.AtualizarSaldoConsolidao;

public record AtualizarSaldoConsolidaoCommand(long ValorLancamento) : ICommand;

