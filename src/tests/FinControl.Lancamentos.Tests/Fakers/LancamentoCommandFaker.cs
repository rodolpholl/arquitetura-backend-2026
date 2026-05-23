using Bogus;
using FinControl.Lancamentos.Core.Domain.Enums;
using FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;

namespace FinControl.Lancamentos.Tests.Fakers;

public static class LancamentoCommandFaker
{
    private static readonly Faker Faker = new("pt_BR");

    public static RegistrarLancamentoCommand ValidVenda(long? valor = null) =>
        Build(ModalidadeLancamento.Venda, valor ?? Faker.Random.Long(100, 999_999));

    public static RegistrarLancamentoCommand ValidDebito(
        ModalidadeLancamento modalidade = ModalidadeLancamento.Devolucao,
        long? valor = null) =>
        Build(modalidade, valor ?? -Faker.Random.Long(100, 999_999));

    public static RegistrarLancamentoCommand ValidOutros(string? descricao = null) =>
        Build(ModalidadeLancamento.Outros, Faker.Random.Long(100, 999_999),
              descricao ?? Faker.Commerce.ProductDescription()[..Math.Min(100, Faker.Commerce.ProductDescription().Length)]);

    public static RegistrarLancamentoCommand Build(
        ModalidadeLancamento modalidade,
        long valor,
        string? descricao = null) =>
        new()
        {
            Modalidade = modalidade,
            Valor = valor,
            Descricao = descricao ?? Faker.Commerce.ProductDescription()[..50],
            DataLancamento = DateTimeOffset.UtcNow.AddDays(-Faker.Random.Int(0, 30)),
            UsuarioId = Guid.NewGuid().ToString(),
            UsuarioNome = Faker.Name.FullName(),
            UsuarioEmail = Faker.Internet.Email(),
            IdempotencyKey = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid()
        };
}
