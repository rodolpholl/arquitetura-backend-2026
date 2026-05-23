namespace FinControl.SharedKernel.Domain.Events;

/// <summary>
/// Contrato de evento compartilhado entre os módulos Lancamentos e Consolidado.
/// Publicado pelo Lancamentos após persistência e consumido pelo Consolidado Worker.
/// </summary>
public record LancamentoRegistradoMessage(
    long Id,
    Guid NavigationId,
    ModalidadeLancamento Modalidade,
    long Valor,
    string? Descricao,
    DateTimeOffset DataLancamento,
    DateTimeOffset OcorridoEm,
    string UsuarioId,
    string UsuarioNome,
    string UsuarioEmail,
    Guid CorrelationId
);

/// <summary>
/// Enum de modalidade para o contrato de evento — valores devem manter paridade
/// com FinControl.Lancamentos.Core.Domain.Enums.ModalidadeLancamento.
/// </summary>
public enum ModalidadeLancamento
{
    Venda = 1,
    Devolucao = 2,
    Suprimento = 3,
    Sangria = 4,
    PagamentoFornecedor = 5,
    RecebimentoDivida = 6,
    Outros = 7
}
