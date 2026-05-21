namespace FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;

/// <summary>
/// Resposta após o registro bem-sucedido de um lançamento.
/// </summary>
public record RegistrarLancamentoResponse(
    /// <summary>
    /// ID único interno do lançamento (BIGINT, gerado pelo banco).
    /// </summary>
    long Id,

    /// <summary>
    /// ID externo do lançamento (UUID, para referências em APIs externas).
    /// </summary>
    Guid NavigationId,

    /// <summary>
    /// Chave de idempotência usada na criação.
    /// </summary>
    Guid IdempotencyKey,

    /// <summary>
    /// ID de correlação para rastreamento distribuído.
    /// </summary>
    Guid CorrelationId,

    /// <summary>
    /// Data/hora de criação do lançamento.
    /// </summary>
    DateTimeOffset CriadoEm
);
