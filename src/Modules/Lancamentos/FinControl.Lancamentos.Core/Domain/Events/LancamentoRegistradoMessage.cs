using FinControl.Lancamentos.Core.Domain.Enums;

namespace FinControl.Lancamentos.Core.Features.Events;

/// <summary>
/// Evento publicado no RabbitMQ (exchange: lancamentos.events, routing key: lancamento.criado)
/// após um lançamento ser persistido com sucesso.
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
