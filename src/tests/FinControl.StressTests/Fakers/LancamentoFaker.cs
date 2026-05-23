using Bogus;

namespace FinControl.StressTests.Fakers;

// Valor em centavos (long), conforme RegistrarLancamentoCommand.Valor
// Modalidades crédito (valor > 0): Venda=1, Suprimento=3, RecebimentoDivida=6
// Modalidades débito  (valor < 0): Devolucao=2, Sangria=4, PagamentoFornecedor=5

internal static class LancamentoFaker
{
    private static readonly Faker Faker = new("pt_BR");

    private static readonly int[] ModalidadesCredito = [1, 6];
    private static readonly int[] ModalidadesDebito  = [2, 3, 4, 5];

    public static LancamentoRequest NextCredito() => new(
        Modalidade:      Faker.PickRandom(ModalidadesCredito),
        Valor:           Faker.Random.Long(100, 1_000_000),   // R$ 1,00 a R$ 10.000,00
        Descricao:       null,
        DataLancamento:  DateTimeOffset.UtcNow,
        UsuarioId:       Guid.NewGuid().ToString(),
        UsuarioNome:     "Stress Test",
        UsuarioEmail:    "stress@fincontrol.test",
        IdempotencyKey:  Guid.NewGuid(),
        CorrelationId:   Guid.NewGuid());

    public static LancamentoRequest NextDebito() => new(
        Modalidade:      Faker.PickRandom(ModalidadesDebito),
        Valor:           Faker.Random.Long(-1_000_000, -100),
        Descricao:       null,
        DataLancamento:  DateTimeOffset.UtcNow,
        UsuarioId:       Guid.NewGuid().ToString(),
        UsuarioNome:     "Stress Test",
        UsuarioEmail:    "stress@fincontrol.test",
        IdempotencyKey:  Guid.NewGuid(),
        CorrelationId:   Guid.NewGuid());

    public static LancamentoRequest Next() =>
        Faker.Random.Bool() ? NextCredito() : NextDebito();
}
