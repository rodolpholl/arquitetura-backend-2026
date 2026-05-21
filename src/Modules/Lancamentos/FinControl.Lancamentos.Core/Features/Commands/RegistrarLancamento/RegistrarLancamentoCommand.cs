using FinControl.Lancamentos.Core.Domain.Enums;
using FinControl.SharedKernel.Messaging;

namespace FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;

/// <summary>
/// Command para registrar um novo lançamento no sistema.
/// Encapsula os dados necessários para criar um lançamento com validação de negócio.
/// </summary>
public record RegistrarLancamentoCommand : ICommand<RegistrarLancamentoResponse>
{
    /// <summary>
    /// Modalidade do lançamento (Venda, Devolução, Suprimento, etc).
    /// </summary>
    public ModalidadeLancamento Modalidade { get; init; }

    /// <summary>
    /// Valor em centavos (para evitar problemas de precisão com decimais).
    /// Ex: 15.50 = 1550 centavos.
    /// </summary>
    public long Valor { get; init; }
    
    /// <summary>
    /// Descrição opcional do lançamento (requerida se Modalidade = Outros).
    /// </summary>
    public string? Descricao { get; init; }

    /// <summary>
    /// Data do lançamento. Se não informada, usa a data atual.
    /// </summary>
    public DateTimeOffset DataLancamento { get; init; }

    /// <summary>
    /// ID do usuário que está criando o lançamento (extraído do Keycloak via context).
    /// </summary>
    public required string UsuarioId { get; init; }

    /// <summary>
    /// Nome completo do usuário (snapshot desnormalizado).
    /// </summary>
    public required string UsuarioNome { get; init; }

    /// <summary>
    /// Email do usuário (snapshot desnormalizado).
    /// </summary>
    public required string UsuarioEmail { get; init; }

    /// <summary>
    /// Chave de idempotência para prevenir duplicação em caso de retry.
    /// </summary>
    public Guid IdempotencyKey { get; init; } = Guid.NewGuid();

    /// <summary>
    /// ID de correlação para rastreamento distribuído.
    /// </summary>
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}
