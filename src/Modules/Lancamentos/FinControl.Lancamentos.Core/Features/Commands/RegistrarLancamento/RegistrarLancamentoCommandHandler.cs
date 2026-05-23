using System.Text.Json;
using FinControl.Lancamentos.Core.Context;
using FinControl.Lancamentos.Core.Domain;
using FinControl.SharedKernel.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;

public class RegistrarLancamentoCommandHandler(
    LancamentosDbContext db,
    ILogger<RegistrarLancamentoCommandHandler> logger)
{
    private const string Exchange = "lancamentos.events";
    private const string RoutingKey = "lancamento.criado";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        // Transação explícita para garantir atomicidade entre lancamento e outbox.
        // O InMemoryDatabase não suporta transações reais; IsRelational() evita a exceção nos testes.
        var useTransaction = db.Database.IsRelational();
        var tx = useTransaction ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        try
        {
            // 1ª gravação: persiste o lancamento e obtém Id + NavigationId gerados pelo banco
            db.Set<Lancamento>().Add(lancamento);
            await db.SaveChangesAsync(cancellationToken);

            // 2ª gravação: persiste o outbox com o payload completo (Id e NavigationId já preenchidos)
            var evento = MapearParaEvento(lancamento, command);
            db.Set<OutboxMessage>().Add(new OutboxMessage
            {
                MessageType = nameof(LancamentoRegistradoMessage),
                Payload = JsonSerializer.Serialize(evento, JsonOptions),
                Exchange = Exchange,
                RoutingKey = RoutingKey,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);

            if (tx is not null)
                await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            if (tx is not null)
                await tx.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (tx is not null)
                await tx.DisposeAsync();
        }

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "LancamentoRegistrado enfileirado no outbox | Exchange={Exchange} RoutingKey={RoutingKey} LancamentoId={LancamentoId} NavigationId={NavigationId} Valor={Valor} CorrelationId={CorrelationId}",
                Exchange, RoutingKey,
                lancamento.Id, lancamento.NavigationId, lancamento.Valor, command.CorrelationId);

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
