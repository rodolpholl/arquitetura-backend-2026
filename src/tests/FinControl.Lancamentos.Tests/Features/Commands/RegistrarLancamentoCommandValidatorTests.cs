using FluentAssertions;
using FinControl.Lancamentos.Core.Domain.Enums;
using FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;
using FinControl.Lancamentos.Tests.Fakers;

namespace FinControl.Lancamentos.Tests.Features.Commands;

public class RegistrarLancamentoCommandValidatorTests
{
    private readonly RegistrarLancamentoCommandValidator _validator = new();

    // ── Happy path ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ModalidadeLancamento.Venda, 1000)]
    [InlineData(ModalidadeLancamento.RecebimentoDivida, 500)]
    public void Deve_Passar_Para_Creditos_Com_Valor_Positivo(ModalidadeLancamento modalidade, long valor)
    {
        var result = _validator.Validate(LancamentoCommandFaker.Build(modalidade, valor));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(ModalidadeLancamento.Devolucao, -500)]
    [InlineData(ModalidadeLancamento.Suprimento, -200)]
    [InlineData(ModalidadeLancamento.PagamentoFornecedor, -1500)]
    [InlineData(ModalidadeLancamento.Sangria, -300)]
    public void Deve_Passar_Para_Debitos_Com_Valor_Negativo(ModalidadeLancamento modalidade, long valor)
    {
        var result = _validator.Validate(LancamentoCommandFaker.Build(modalidade, valor));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Deve_Passar_Para_Outros_Com_Descricao()
    {
        var result = _validator.Validate(LancamentoCommandFaker.ValidOutros("Pagamento avulso de serviço"));

        result.IsValid.Should().BeTrue();
    }

    // ── Modalidade ──────────────────────────────────────────────────────────

    [Fact]
    public void Deve_Falhar_Quando_Modalidade_Invalida()
    {
        var command = LancamentoCommandFaker.ValidVenda() with { Modalidade = (ModalidadeLancamento)99 };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Modalidade));
    }

    // ── Valor ───────────────────────────────────────────────────────────────

    [Fact]
    public void Deve_Falhar_Quando_Valor_Excede_Maximo()
    {
        var command = LancamentoCommandFaker.ValidVenda() with { Valor = 100_000_000_000_000L };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Valor));
    }

    [Theory]
    [InlineData(ModalidadeLancamento.Devolucao, 500)]
    [InlineData(ModalidadeLancamento.Suprimento, 100)]
    [InlineData(ModalidadeLancamento.PagamentoFornecedor, 1000)]
    [InlineData(ModalidadeLancamento.Sangria, 750)]
    public void Deve_Falhar_Quando_Valor_Positivo_Para_Modalidade_Debito(ModalidadeLancamento modalidade, long valor)
    {
        var command = LancamentoCommandFaker.Build(modalidade, valor);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Valor));
    }

    [Theory]
    [InlineData(ModalidadeLancamento.Venda, -500)]
    [InlineData(ModalidadeLancamento.RecebimentoDivida, -100)]
    public void Deve_Falhar_Quando_Valor_Negativo_Para_Modalidade_Credito(ModalidadeLancamento modalidade, long valor)
    {
        var command = LancamentoCommandFaker.Build(modalidade, valor);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Valor));
    }

    // ── Descrição ───────────────────────────────────────────────────────────

    [Fact]
    public void Deve_Falhar_Quando_Modalidade_Outros_Sem_Descricao()
    {
        var command = LancamentoCommandFaker.Build(ModalidadeLancamento.Outros, 500, descricao: null)
            with { Descricao = null };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Descricao));
    }

    [Fact]
    public void Deve_Falhar_Quando_Descricao_Excede_500_Caracteres()
    {
        var command = LancamentoCommandFaker.ValidVenda() with { Descricao = new string('x', 501) };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Descricao));
    }

    // ── Data ────────────────────────────────────────────────────────────────

    [Fact]
    public void Deve_Falhar_Quando_DataLancamento_Mais_De_1_Dia_No_Futuro()
    {
        var command = LancamentoCommandFaker.ValidVenda() with
        {
            DataLancamento = DateTimeOffset.UtcNow.AddDays(2)
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.DataLancamento));
    }

    [Fact]
    public void Deve_Falhar_Quando_DataLancamento_Ha_Mais_De_Um_Ano()
    {
        var command = LancamentoCommandFaker.ValidVenda() with
        {
            DataLancamento = DateTimeOffset.UtcNow.AddYears(-2)
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.DataLancamento));
    }

    // ── Usuário ─────────────────────────────────────────────────────────────

    [Fact]
    public void Deve_Falhar_Quando_UsuarioId_Vazio()
    {
        var command = LancamentoCommandFaker.ValidVenda() with { UsuarioId = string.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.UsuarioId));
    }

    [Fact]
    public void Deve_Falhar_Quando_UsuarioId_Nao_Tem_36_Caracteres()
    {
        var command = LancamentoCommandFaker.ValidVenda() with { UsuarioId = "usuario-curto" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.UsuarioId));
    }

    [Fact]
    public void Deve_Falhar_Quando_UsuarioNome_Vazio()
    {
        var command = LancamentoCommandFaker.ValidVenda() with { UsuarioNome = string.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.UsuarioNome));
    }

    [Fact]
    public void Deve_Falhar_Quando_UsuarioNome_Excede_200_Caracteres()
    {
        var command = LancamentoCommandFaker.ValidVenda() with { UsuarioNome = new string('a', 201) };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.UsuarioNome));
    }

    // ── Email ───────────────────────────────────────────────────────────────

    [Fact]
    public void Deve_Falhar_Quando_Email_Invalido()
    {
        var command = LancamentoCommandFaker.ValidVenda() with { UsuarioEmail = "nao-e-email" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.UsuarioEmail));
    }

    [Fact]
    public void Deve_Falhar_Quando_Email_Vazio()
    {
        var command = LancamentoCommandFaker.ValidVenda() with { UsuarioEmail = string.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.UsuarioEmail));
    }

    // ── Idempotência / Correlação ────────────────────────────────────────────

    [Fact]
    public void Deve_Falhar_Quando_IdempotencyKey_For_Empty_Guid()
    {
        var command = LancamentoCommandFaker.ValidVenda() with { IdempotencyKey = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.IdempotencyKey));
    }

    [Fact]
    public void Deve_Falhar_Quando_CorrelationId_For_Empty_Guid()
    {
        var command = LancamentoCommandFaker.ValidVenda() with { CorrelationId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.CorrelationId));
    }
}
