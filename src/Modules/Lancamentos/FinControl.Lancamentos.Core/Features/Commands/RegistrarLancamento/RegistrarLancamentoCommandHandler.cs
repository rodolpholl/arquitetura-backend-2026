using FinControl.Lancamentos.Core.Context;
using FinControl.Lancamentos.Core.Domain;

namespace FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;

/// <summary>
/// Handler para o comando RegistrarLancamento.
/// Usa constructor injection para LancamentosDbContext — resolve via DI padrão,
/// sem depender do code gen de method-parameter do Wolverine EF Core.
/// </summary>
public class RegistrarLancamentoCommandHandler(LancamentosDbContext db)
{
    public async Task<RegistrarLancamentoResponse> Handle(
        RegistrarLancamentoCommand command,
        CancellationToken cancellationToken = default)
    {
        var existente = await VerificarIdempotenciaAsync(command.IdempotencyKey, cancellationToken);
        if (existente is not null)
            return MapearParaResponse(existente);

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

        return MapearParaResponse(lancamento);
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
            Id: lancamento.Id,
            NavigationId: lancamento.NavigationId ?? Guid.NewGuid(),
            IdempotencyKey: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            CriadoEm: lancamento.CreatedAt
        );
}
