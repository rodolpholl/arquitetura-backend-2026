using Bogus;
using FinControl.Consolidado.Core.Domain;

namespace FinControl.Consolidado.Tests.Fakers;

public static class SaldoConsolidadoFaker
{
    private static readonly Faker Faker = new("pt_BR");

    public static SaldoConsolidado Positivo(long? saldo = null) =>
        new(saldo ?? Faker.Random.Long(100, 999_999),
            DateTimeOffset.UtcNow.AddMinutes(-Faker.Random.Int(1, 60)));

    public static SaldoConsolidado Negativo(long? saldo = null) =>
        new(saldo ?? -Faker.Random.Long(100, 999_999),
            DateTimeOffset.UtcNow.AddMinutes(-Faker.Random.Int(1, 60)));

    public static SaldoConsolidado Zero() =>
        new(0, DateTimeOffset.UtcNow);
}
