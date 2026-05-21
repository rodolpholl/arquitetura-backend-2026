using FinControl.Infrastructure.Repositories;
using FinControl.Lancamentos.Core.Context;
using FinControl.Lancamentos.Core.Domain;
using Wolverine.Attributes;

namespace FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;

/// <summary>
/// Handler para o comando RegistrarLancamento.
/// Implementa a lógica de negócio para criar um novo lançamento.
/// 
/// Padrão Wolverine: Descoberta por convenção (não requer interface).
/// O método "Handle" é descoberto automaticamente pelo Wolverine.
/// </summary>
[Transactional]
public class RegistrarLancamentoCommandHandler
{
    private readonly IRepository<Lancamento> _repository;
    private readonly LancamentosDbContext _context;

    public RegistrarLancamentoCommandHandler(
        IRepository<Lancamento> repository,
        LancamentosDbContext context)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Executa a criação do lançamento com tratamento de idempotência.
    /// </summary>
    public async Task<RegistrarLancamentoResponse> Handle(
        RegistrarLancamentoCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Validar idempotência - se já existe, retorna o existente
        var lancamentoExistente = await VerificarIdempotenciaAsync(command.IdempotencyKey, cancellationToken);
        if (lancamentoExistente is not null)
        {
            return MapearParaResponse(lancamentoExistente);
        }

        // 2. Criar nova entidade Lancamento usando factory method
        var lancamento = Lancamento.Criar(
            modalidade: command.Modalidade,
            valor: command.Valor,
            descricao: command.Descricao,
            dataLancamento: command.DataLancamento == default ? null : command.DataLancamento
        );

        // 3. Preencher dados de auditoria
        lancamento.CreatedAt = DateTimeOffset.UtcNow;
        lancamento.CreatedBy = command.UsuarioId;
        lancamento.CreatedByName = command.UsuarioNome;
        lancamento.CreatedByEmail = command.UsuarioEmail;

        // 4. Inserir no repositório
        await _repository.AddAsync(lancamento, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // 5. Retornar response com IDs gerados
        return MapearParaResponse(lancamento);
    }

    /// <summary>
    /// Verifica se já existe um lançamento com a mesma chave de idempotência.
    /// </summary>
    private static async Task<Lancamento?> VerificarIdempotenciaAsync(Guid idempotencyKey, CancellationToken cancellationToken)
    {
        // Implementação futura: query na tabela de idempotência ou no próprio lançamento
        // Por enquanto, sempre permite nova criação
        // TODO: adicionar campo IdempotencyKey na entidade e no mapeamento
        await Task.Delay(0, cancellationToken); // placeholder
        return null;
    }

    /// <summary>
    /// Mapeia a entidade Lancamento para o DTO de resposta.
    /// </summary>
    private static RegistrarLancamentoResponse MapearParaResponse(Lancamento lancamento)
    {
        return new RegistrarLancamentoResponse(
            Id: lancamento.Id,
            NavigationId: lancamento.NavigationId ?? Guid.NewGuid(),
            IdempotencyKey: Guid.NewGuid(), // TODO: retornar IdempotencyKey da entidade
            CorrelationId: Guid.NewGuid(), // TODO: retornar CorrelationId da entidade
            CriadoEm: lancamento.CreatedAt
        );
    }
}
