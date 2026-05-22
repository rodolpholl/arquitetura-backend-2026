using FinControl.Lancamentos.Core.Context;
using FinControl.Lancamentos.Core.Domain;
using FinControl.Lancamentos.Core.Features.Events;
using Microsoft.Extensions.Logging;

namespace FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;

/// <summary>
/// Handler para o comando RegistrarLancamento.
/// Usa constructor injection para LancamentosDbContext — resolve via DI padrão,
/// sem depender do code gen de method-parameter do Wolverine EF Core.
/// </summary>
public class RegistrarLancamentoCommandHandler(
    LancamentosDbContext db,
    ILogger<RegistrarLancamentoCommandHandler> logger)
{
    public async Task<(RegistrarLancamentoResponse, LancamentoRegistradoMessage?)> Handle(
        RegistrarLancamentoCommand command,
        CancellationToken cancellationToken = default)
    {
        var existente = await VerificarIdempotenciaAsync(command.IdempotencyKey, cancellationToken);
        if (existente is not null)
            return (MapearParaResponse(existente), null);


        Lancamento lancamento = new()
        {
            Modalidade = command.Modalidade,
            Valor = command.Valor,
            Descricao = command.Descricao,
            DataLancamento = command.DataLancamento == default ? DateTimeOffset.UtcNow : command.DataLancamento,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = command.UsuarioId,
            CreatedByName = command.UsuarioNome,
            CreatedByEmail = command.UsuarioEmail
        };
        

        db.Set<Lancamento>().Add(lancamento);
        await db.SaveChangesAsync(cancellationToken);

        var evento = MapearParaEvento(lancamento, command);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Mensagem {EventType} publicada para o RabbitMQ | Exchange={Exchange} RoutingKey={RoutingKey} Queue={Queue} NavigationId={NavigationId} Modalidade={Modalidade} Valor={Valor} UsuarioId={UsuarioId} CorrelationId={CorrelationId}",
                nameof(LancamentoRegistradoMessage),
                "lancamentos.events",
                "lancamento.criado",
                "lancamentos.registered",
                evento.NavigationId,
                evento.Modalidade,
                evento.Valor,
                evento.UsuarioId,
                evento.CorrelationId);

        return (MapearParaResponse(lancamento), evento);
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
