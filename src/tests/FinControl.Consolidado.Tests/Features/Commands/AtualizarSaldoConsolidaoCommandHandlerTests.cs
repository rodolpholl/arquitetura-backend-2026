using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using FinControl.Consolidado.Core.Domain;
using FinControl.Consolidado.Core.Features.Commands.AtualizarSaldoConsolidao;
using FinControl.Consolidado.Tests.Fakers;
using FinControl.Infrastructure.Cache;

namespace FinControl.Consolidado.Tests.Features.Commands;

public class AtualizarSaldoConsolidaoCommandHandlerTests
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static RedisCacheService CacheService(Mock<IDistributedCache> mock) =>
        new(mock.Object, NullLogger<RedisCacheService>.Instance);

    private static byte[] ToBytes<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value, JsonOpts);

    private static SaldoConsolidado? FromBytes(byte[]? bytes) =>
        bytes is null ? null : JsonSerializer.Deserialize<SaldoConsolidado>(bytes, JsonOpts);

    private static (AtualizarSaldoConsolidaoCommandHandler handler, Mock<IDistributedCache> cacheMock)
        CreateHandler(SaldoConsolidado? saldoAtual = null)
    {
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(saldoAtual is null ? null : ToBytes(saldoAtual));

        var handler = new AtualizarSaldoConsolidaoCommandHandler(
            CacheService(cacheMock),
            NullLogger<AtualizarSaldoConsolidaoCommandHandler>.Instance);

        return (handler, cacheMock);
    }

    private static byte[]? CaptureSavedBytes(Mock<IDistributedCache> cacheMock)
    {
        byte[]? saved = null;
        cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (_, bytes, _, _) => saved = bytes)
            .Returns(Task.CompletedTask);
        return saved; // retornará null até o callback ser invocado — use o ref capturado pelo lambda
    }

    // ── Criação do saldo ─────────────────────────────────────────────────────

    [Fact]
    public async Task Deve_Criar_Saldo_Inicial_Quando_Cache_Vazio()
    {
        var (handler, cacheMock) = CreateHandler(saldoAtual: null);

        byte[]? savedBytes = null;
        cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (_, b, _, _) => savedBytes = b)
            .Returns(Task.CompletedTask);

        await handler.Handle(new AtualizarSaldoConsolidaoCommand(5000));

        var salvo = FromBytes(savedBytes);
        salvo.Should().NotBeNull();
        salvo!.Saldo.Should().Be(5000);
    }

    // ── Acumulação ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Deve_Acumular_Sobre_Saldo_Existente()
    {
        var (handler, cacheMock) = CreateHandler(SaldoConsolidadoFaker.Positivo(10000));

        byte[]? savedBytes = null;
        cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (_, b, _, _) => savedBytes = b)
            .Returns(Task.CompletedTask);

        await handler.Handle(new AtualizarSaldoConsolidaoCommand(3000));

        FromBytes(savedBytes)!.Saldo.Should().Be(13000);
    }

    [Fact]
    public async Task Deve_Decrementar_Saldo_Para_Lancamento_Negativo()
    {
        var (handler, cacheMock) = CreateHandler(SaldoConsolidadoFaker.Positivo(10000));

        byte[]? savedBytes = null;
        cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (_, b, _, _) => savedBytes = b)
            .Returns(Task.CompletedTask);

        await handler.Handle(new AtualizarSaldoConsolidaoCommand(-4000));

        FromBytes(savedBytes)!.Saldo.Should().Be(6000);
    }

    [Fact]
    public async Task Saldo_Pode_Ficar_Negativo_Quando_Debitos_Superam_Creditos()
    {
        var (handler, cacheMock) = CreateHandler(SaldoConsolidadoFaker.Positivo(1000));

        byte[]? savedBytes = null;
        cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (_, b, _, _) => savedBytes = b)
            .Returns(Task.CompletedTask);

        await handler.Handle(new AtualizarSaldoConsolidaoCommand(-5000));

        FromBytes(savedBytes)!.Saldo.Should().Be(-4000);
    }

    // ── TTL ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Deve_Salvar_Com_TTL_De_30_Dias()
    {
        var (handler, cacheMock) = CreateHandler();

        DistributedCacheEntryOptions? capturedOpts = null;
        cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (_, _, opts, _) => capturedOpts = opts)
            .Returns(Task.CompletedTask);

        await handler.Handle(new AtualizarSaldoConsolidaoCommand(1000));

        capturedOpts.Should().NotBeNull();
        capturedOpts!.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromDays(30));
    }

    // ── Chave de cache ───────────────────────────────────────────────────────

    [Fact]
    public async Task Deve_Usar_Chave_Com_Data_De_Hoje()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var keyEsperada = $"saldo:consolidado:{hoje:yyyy-MM-dd}";

        var (handler, cacheMock) = CreateHandler();
        cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await handler.Handle(new AtualizarSaldoConsolidaoCommand(500));

        cacheMock.Verify(c => c.SetAsync(
            keyEsperada,
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── UltimaAtualizacao ────────────────────────────────────────────────────

    [Fact]
    public async Task Deve_Atualizar_UltimaAtualizacao_Para_Agora()
    {
        var (handler, cacheMock) = CreateHandler(SaldoConsolidadoFaker.Positivo(1000));

        byte[]? savedBytes = null;
        cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (_, b, _, _) => savedBytes = b)
            .Returns(Task.CompletedTask);

        var antes = DateTimeOffset.UtcNow;
        await handler.Handle(new AtualizarSaldoConsolidaoCommand(500));

        FromBytes(savedBytes)!.UltimaAtualizacao.Should().BeOnOrAfter(antes);
    }
}
