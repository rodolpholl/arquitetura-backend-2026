namespace FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;

/// <summary>
/// Resposta após o registro bem-sucedido de um lançamento.
/// </summary>
public record RegistrarLancamentoResponse(

    /// <summary>
    /// ID externo do lançamento (UUID, para referências em APIs externas).
    /// </summary>
    Guid NavigationId,

    /// <summary>
    /// Data/hora de criação do lançamento.
    /// </summary>
    DateTimeOffset CriadoEm
);
