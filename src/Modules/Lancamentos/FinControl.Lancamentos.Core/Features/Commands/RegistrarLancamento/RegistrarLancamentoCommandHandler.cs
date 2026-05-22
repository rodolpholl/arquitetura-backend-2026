using FinControl.Lancamentos.Core.Context;
using FinControl.Lancamentos.Core.Domain;
using FinControl.Lancamentos.Core.Features.Events;

namespace FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;

/// <summary>
/// Handler para o comando RegistrarLancamento.
/// Usa constructor injection para LancamentosDbContext — resolve via DI padrão,
/// sem depender do code gen de method-parameter do Wolverine EF Core.
/// </summary>
public class RegistrarLancamentoCommandHandler(LancamentosDbContext db)
{
    public async Task<(RegistrarLancamentoResponse, LancamentoRegistradoMessage?)> Handle(
        RegistrarLancamentoCommand command,
        CancellationToken cancellationToken = default)
    {
        var existente = await VerificarIdempotenciaAsync(command.IdempotencyKey, cancellationToken);
        if (existente is not null)
            return (MapearParaResponse(existente), null);

        var lancamento = Lancamento.Criar(
            modalidade: command.Modalidade,
            valor: command.Valor,
            descricao: command.Descricao,
            dataLancamento: command.DataLancamento == default ? null : command.DataLancamento
        );

        lancamento.CreatedAt = DateTimeOffset.UtcNow;
        lancamento.CreatedBy = command.UsuarioId;
        lancamento.CreatedByName = command.UsuarioNome;
        lancamento.CreatedByEmail = command.UsuarioEmail;

        db.Set<Lancamento>().Add(lancamento);
        await db.SaveChangesAsync(cancellationToken);

        return (MapearParaResponse(lancamento), MapearParaEvento(lancamento, command));
    }

    private static Task<Lancamento?> VerificarIdempotenciaAsync(
        Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        // TODO: implementar busca por IdempotencyKey quando o campo for adicionado à entidade
        _ = idempotencyKey;
        _ = cancellationToken;
        return Task.FromResult<Lancamento?>(null);
    }

    private static RegistrarLancamentoResponse MapearParaResponse(Lancamento lancamento) =>
        new(
            NavigationId: lancamento.NavigationId ?? Guid.NewGuid(),
            CriadoEm: lancamento.CreatedAt
        );

    private static LancamentoRegistradoMessage MapearParaEvento(
        Lancamento lancamento,
        RegistrarLancamentoCommand command) =>
        new(
            Id: lancamento.Id,
            NavigationId: lancamento.NavigationId ?? Guid.NewGuid(),
            Modalidade: lancamento.Modalidade,
            Valor: lancamento.Valor,
            Descricao: lancamento.Descricao,
            DataLancamento: lancamento.DataLancamento,
            OcorridoEm: lancamento.CreatedAt,
            UsuarioId: command.UsuarioId,
            UsuarioNome: command.UsuarioNome,
            UsuarioEmail: command.UsuarioEmail,
            CorrelationId: command.CorrelationId
        );
}
