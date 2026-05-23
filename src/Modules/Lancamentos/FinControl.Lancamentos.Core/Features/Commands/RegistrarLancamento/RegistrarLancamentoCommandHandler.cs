using FinControl.Infrastructure.Messaging;
using FinControl.Lancamentos.Core.Context;
using FinControl.Lancamentos.Core.Domain;
using FinControl.SharedKernel.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;

public class RegistrarLancamentoCommandHandler(
    LancamentosDbContext db,
    IRabbitMqPublisher publisher,
    ILogger<RegistrarLancamentoCommandHandler> logger)
{
    private const string Exchange = "lancamentos.events";
    private const string RoutingKey = "lancamento.criado";

    public async Task<RegistrarLancamentoResponse> Handle(
        RegistrarLancamentoCommand command,
        CancellationToken cancellationToken = default)
    {
        var existente = await VerificarIdempotenciaAsync(command.IdempotencyKey, cancellationToken);
        if (existente is not null)
            return MapearParaResponse(existente);

        Lancamento lancamento = new()
        {
            Modalidade = command.Modalidade,
            Valor = command.Valor,
            Descricao = command.Descricao,
            DataLancamento = command.DataLancamento == default ? DateTimeOffset.UtcNow : command.DataLancamento,
            IdempotencyKey = command.IdempotencyKey,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = command.UsuarioId,
            CreatedByName = command.UsuarioNome,
            CreatedByEmail = command.UsuarioEmail
        };

        db.Set<Lancamento>().Add(lancamento);
        await db.SaveChangesAsync(cancellationToken);

        var evento = MapearParaEvento(lancamento, command);

        await publisher.PublishAsync(evento, Exchange, RoutingKey, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "LancamentoRegistrado publicado | Exchange={Exchange} RoutingKey={RoutingKey} NavigationId={NavigationId} Modalidade={Modalidade} Valor={Valor} CorrelationId={CorrelationId}",
                Exchange, RoutingKey,
                evento.NavigationId, evento.Modalidade, evento.Valor, evento.CorrelationId);

        return MapearParaResponse(lancamento);
    }

    private Task<Lancamento?> VerificarIdempotenciaAsync(
        Guid idempotencyKey,
        CancellationToken cancellationToken) =>
        db.Lancamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.IdempotencyKey == idempotencyKey, cancellationToken);

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
            Modalidade: (ModalidadeLancamento)(int)lancamento.Modalidade,
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
