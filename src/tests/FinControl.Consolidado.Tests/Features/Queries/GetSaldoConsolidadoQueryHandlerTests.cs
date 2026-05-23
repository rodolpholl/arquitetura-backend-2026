using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using FinControl.Consolidado.Core.Domain;
using FinControl.Consolidado.Core.Features.Queries.GetSaldoConsolidado;
using FinControl.Consolidado.Tests.Fakers;
using FinControl.Infrastructure.Cache;

namespace FinControl.Consolidado.Tests.Features.Queries;

public class GetSaldoConsolidadoQueryHandlerTests
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static RedisCacheService CacheService(Mock<IDistributedCache> mock) =>
        new(mock.Object, NullLogger<RedisCacheService>.Instance);

    private static byte[] ToBytes<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value, JsonOpts);

    private static GetSaldoConsolidadoQueryHandler CreateHandler(Mock<IDistributedCache> cacheMock) =>
        new(CacheService(cacheMock),
            NullLogger<GetSaldoConsolidadoQueryHandler>.Instance);

    // ── Cache hit ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Deve_Retornar_Saldo_Quando_Existe_No_Cache()
    {
        var saldo = SaldoConsolidadoFaker.Positivo(15000);
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(ToBytes(saldo));

        var response = await CreateHandler(cacheMock)
            .Handle(new GetSaldoConsolidadoQuery(), CancellationToken.None);

        response.Saldo.Should().Be(15000);
    }

    [Fact]
    public async Task Deve_Converter_Centavos_Para_Decimal_Corretamente()
    {
        var saldo = new SaldoConsolidado(15550, DateTimeOffset.UtcNow);
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(ToBytes(saldo));

        var response = await CreateHandler(cacheMock)
            .Handle(new GetSaldoConsolidadoQuery(), CancellationToken.None);

        response.SaldoDecimal.Should().Be(155.50m);
    }

    [Fact]
    public async Task Deve_Retornar_UltimaAtualizacao_Do_Cache()
    {
        var quando = DateTimeOffset.UtcNow.AddMinutes(-10);
        var saldo = new SaldoConsolidado(1000, quando);
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(ToBytes(saldo));

        var response = await CreateHandler(cacheMock)
            .Handle(new GetSaldoConsolidadoQuery(), CancellationToken.None);

        response.UltimaAtualizacao.Should().BeCloseTo(quando, TimeSpan.FromSeconds(1));
    }

    // ── Cache miss ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Deve_Retornar_Saldo_Zero_Quando_Cache_Vazio()
    {
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((byte[]?)null);

        var response = await CreateHandler(cacheMock)
            .Handle(new GetSaldoConsolidadoQuery(), CancellationToken.None);

        response.Saldo.Should().Be(0);
        response.SaldoDecimal.Should().Be(0m);
    }

    // ── Chave de cache ───────────────────────────────────────────────────────

    [Fact]
    public async Task Deve_Consultar_Cache_Com_Data_Especifica_Formatada()
    {
        var data = new DateOnly(2026, 5, 23);
        var expectedKey = "saldo:consolidado:2026-05-23";

        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((byte[]?)null);

        await CreateHandler(cacheMock).Handle(new GetSaldoConsolidadoQuery(data), CancellationToken.None);

        cacheMock.Verify(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deve_Consultar_Cache_Com_Data_De_Hoje_Quando_Nao_Informada()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var keyHoje = $"saldo:consolidado:{hoje:yyyy-MM-dd}";

        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((byte[]?)null);

        await CreateHandler(cacheMock).Handle(new GetSaldoConsolidadoQuery(), CancellationToken.None);

        cacheMock.Verify(c => c.GetAsync(keyHoje, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Saldo negativo ───────────────────────────────────────────────────────

    [Fact]
    public async Task Deve_Retornar_Saldo_Negativo_Quando_Cache_Contem_Debito_Maior()
    {
        var saldo = SaldoConsolidadoFaker.Negativo(-5000);
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(ToBytes(saldo));

        var response = await CreateHandler(cacheMock)
            .Handle(new GetSaldoConsolidadoQuery(), CancellationToken.None);

        response.Saldo.Should().BeNegative();
        response.SaldoDecimal.Should().BeNegative();
    }
}
