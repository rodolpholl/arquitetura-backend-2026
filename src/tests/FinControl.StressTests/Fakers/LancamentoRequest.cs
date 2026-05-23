using System.Text.Json.Serialization;

namespace FinControl.StressTests.Fakers;

// DTO espelho de RegistrarLancamentoCommand (sem referência ao projeto Core).
// Propriedades em camelCase via JsonPropertyName para garantir serialização correta.
internal sealed record LancamentoRequest(
    [property: JsonPropertyName("modalidade")]     int             Modalidade,
    [property: JsonPropertyName("valor")]          long            Valor,
    [property: JsonPropertyName("descricao")]      string?         Descricao,
    [property: JsonPropertyName("dataLancamento")] DateTimeOffset  DataLancamento,
    [property: JsonPropertyName("usuarioId")]      string          UsuarioId,
    [property: JsonPropertyName("usuarioNome")]    string          UsuarioNome,
    [property: JsonPropertyName("usuarioEmail")]   string          UsuarioEmail,
    [property: JsonPropertyName("idempotencyKey")] Guid            IdempotencyKey,
    [property: JsonPropertyName("correlationId")]  Guid            CorrelationId);
