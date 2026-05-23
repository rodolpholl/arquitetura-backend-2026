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

    private static readonly DateTimeOffset DataPadrao =
        new(2026, 5, 23, 10, 0, 0, TimeSpan.Zero);

    private static RedisCacheService CacheService(Mock<IDistributedCache> mock) =>
        new(mock.Object, NullLogger<RedisCacheService>.Instance);

    private static byte[] ToBytes<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value, JsonOpts);

    private static SaldoConsolidado? FromBytes(byte[]? bytes) =>
        bytes is null ? null : JsonSerializer.Deserialize<SaldoConsolidado>(bytes, JsonOpts);

    // IRedisLockService que executa a action imediatamente (sem Redis real nos testes)
    private static Mock<IRedisLockService> LockMockPassthrough()
    {
        var m = new Mock<IRedisLockService>();
        m.Setup(l => l.ExecuteWithLockAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task>, TimeSpan, CancellationToken>(
                async (_, action, _, _) => { await action(); return true; });
        return m;
    }

    private static (AtualizarSaldoConsolidadoCommandHandler handler, Mock<IDistributedCache> cacheMock)
        CreateHandler(SaldoConsolidado? saldoAtual = null)
    {
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(saldoAtual is null ? null : ToBytes(saldoAtual));

        var handler = new AtualizarSaldoConsolidadoCommandHandler(
            CacheService(cacheMock),
            LockMockPassthrough().Object,
            NullLogger<AtualizarSaldoConsolidadoCommandHandler>.Instance);

        return (handler, cacheMock);
    }

    private static AtualizarSaldoConsolidadoCommand Cmd(long valor) =>
        new(ValorLancamento: valor, DataLancamento: DataPadrao);

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
        return saved;
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

        await handler.Handle(Cmd(5000));

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

        await handler.Handle(Cmd(3000));

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

        await handler.Handle(Cmd(-4000));

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

        await handler.Handle(Cmd(-5000));

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

        await handler.Handle(Cmd(1000));

        capturedOpts.Should().NotBeNull();
        capturedOpts!.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromDays(30));
    }

    // ── Chave de cache usa data do lançamento (não UtcNow) ───────────────────

    [Fact]
    public async Task Deve_Usar_Chave_Com_Data_Do_Lancamento()
    {
        var dataLancamento = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);
        var keyEsperada = "saldo:consolidado:2026-01-15";

        var (handler, cacheMock) = CreateHandler();
        cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await handler.Handle(new AtualizarSaldoConsolidadoCommand(500, dataLancamento));

        cacheMock.Verify(c => c.SetAsync(
            keyEsperada,
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Lançamento retroativo vai para o dia correto ──────────────────────────

    [Fact]
    public async Task Deve_Usar_Data_Do_Lancamento_Para_Entrada_Retroativa()
    {
        var dataRetroativa = new DateTimeOffset(2025, 12, 31, 23, 59, 0, TimeSpan.Zero);
        var keyEsperada = "saldo:consolidado:2025-12-31";

        var (handler, cacheMock) = CreateHandler();
        cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await handler.Handle(new AtualizarSaldoConsolidadoCommand(1000, dataRetroativa));

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
        await handler.Handle(Cmd(500));

        FromBytes(savedBytes)!.UltimaAtualizacao.Should().BeOnOrAfter(antes);
    }

    // ── Lock distribuído é invocado ───────────────────────────────────────────

    [Fact]
    public async Task Deve_Invocar_Lock_Distribuido_Para_Cada_Atualizacao()
    {
        var (_, cacheMock) = CreateHandler();
        var lockMock = LockMockPassthrough();

        cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new AtualizarSaldoConsolidadoCommandHandler(
            CacheService(cacheMock),
            lockMock.Object,
            NullLogger<AtualizarSaldoConsolidadoCommandHandler>.Instance);

        await handler.Handle(Cmd(1000));

        lockMock.Verify(l => l.ExecuteWithLockAsync(
            It.Is<string>(k => k.Contains("lock:saldo:consolidado")),
            It.IsAny<Func<Task>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
