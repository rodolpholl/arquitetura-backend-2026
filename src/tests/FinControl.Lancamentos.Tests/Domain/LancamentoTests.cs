using FluentAssertions;
using FinControl.Lancamentos.Core.Domain;
using FinControl.Lancamentos.Core.Domain.Enums;

namespace FinControl.Lancamentos.Tests.Domain;

public class LancamentoTests
{
    [Fact]
    public void Tipo_Deve_Ser_Credito_Para_Valor_Positivo()
    {
        var lancamento = new Lancamento { Valor = 1000, Modalidade = ModalidadeLancamento.Venda };

        lancamento.Tipo.Should().Be(TipoLancamento.Credito);
    }

    [Fact]
    public void Tipo_Deve_Ser_Debito_Para_Valor_Negativo()
    {
        var lancamento = new Lancamento { Valor = -1000, Modalidade = ModalidadeLancamento.Devolucao };

        lancamento.Tipo.Should().Be(TipoLancamento.Debito);
    }

    [Theory]
    [InlineData(15550, 155.50)]
    [InlineData(100, 1.00)]
    [InlineData(1, 0.01)]
    [InlineData(100000, 1000.00)]
    public void ValorFormatado_Deve_Converter_Centavos_Para_Reais(long centavos, double esperado)
    {
        var lancamento = new Lancamento { Valor = centavos };

        lancamento.ValorFormatado.Should().Be((decimal)esperado);
    }

    [Fact]
    public void ValorFormatado_Deve_Manter_Sinal_Negativo()
    {
        var lancamento = new Lancamento { Valor = -5000 };

        lancamento.ValorFormatado.Should().Be(-50.00m);
    }

    [Fact]
    public void Dois_Lancamentos_Com_Mesmo_Id_Devem_Ser_Iguais()
    {
        var a = new Lancamento { Modalidade = ModalidadeLancamento.Venda, Valor = 1000 };
        var b = new Lancamento { Modalidade = ModalidadeLancamento.Devolucao, Valor = -500 };

        // Id padrão é 0 (default long) em ambos — igualdade por Id
        a.Should().Be(b);
    }

    [Fact]
    public void Lancamento_Deve_Ter_CreatedAt_Preenchido_Por_Padrao()
    {
        var antes = DateTimeOffset.UtcNow;
        var lancamento = new Lancamento { Valor = 500, Modalidade = ModalidadeLancamento.Venda };
        var depois = DateTimeOffset.UtcNow;

        lancamento.CreatedAt.Should().BeOnOrAfter(antes).And.BeOnOrBefore(depois);
    }
}
