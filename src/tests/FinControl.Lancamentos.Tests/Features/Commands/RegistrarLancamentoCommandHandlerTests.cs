using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using FinControl.Lancamentos.Core.Context;
using FinControl.Lancamentos.Core.Domain;
using FinControl.Lancamentos.Core.Domain.Enums;
using FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;
using FinControl.Lancamentos.Tests.Fakers;
using SharedKernelEvents = FinControl.SharedKernel.Domain.Events;

namespace FinControl.Lancamentos.Tests.Features.Commands;

public class RegistrarLancamentoCommandHandlerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static LancamentosDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<LancamentosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static RegistrarLancamentoCommandHandler CreateHandler(LancamentosDbContext db) =>
        new(db, NullLogger<RegistrarLancamentoCommandHandler>.Instance);

    // ── Persistência ────────────────────────────────────────────────────────

    [Fact]
    public async Task Deve_Salvar_Lancamento_No_Banco()
    {
        await using var db = CreateDbContext();
        var handler = CreateHandler(db);

        await handler.Handle(LancamentoCommandFaker.ValidVenda(1500));

        db.Lancamentos.Should().HaveCount(1);
    }

    [Fact]
    public async Task Deve_Retornar_NavigationId_Valido()
    {
        await using var db = CreateDbContext();
        var handler = CreateHandler(db);

        var response = await handler.Handle(LancamentoCommandFaker.ValidVenda());

        response.NavigationId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Deve_Retornar_CriadoEm_Recente()
    {
        await using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var antes = DateTimeOffset.UtcNow;

        var response = await handler.Handle(LancamentoCommandFaker.ValidVenda());

        response.CriadoEm.Should().BeOnOrAfter(antes);
    }

    [Fact]
    public async Task Deve_Persistir_Dados_Do_Comando_No_Lancamento()
    {
        await using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = LancamentoCommandFaker.Build(ModalidadeLancamento.Venda, 2500);

        await handler.Handle(command);

        var salvo = db.Lancamentos.Single();
        salvo.Valor.Should().Be(command.Valor);
        salvo.Modalidade.Should().Be(command.Modalidade);
        salvo.Descricao.Should().Be(command.Descricao);
        salvo.CreatedBy.Should().Be(command.UsuarioId);
        salvo.CreatedByName.Should().Be(command.UsuarioNome);
        salvo.CreatedByEmail.Should().Be(command.UsuarioEmail);
    }

    [Fact]
    public async Task Deve_Usar_DataLancamento_Atual_Quando_Default()
    {
        await using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = LancamentoCommandFaker.ValidVenda() with { DataLancamento = default };
        var antes = DateTimeOffset.UtcNow;

        await handler.Handle(command);

        var salvo = db.Lancamentos.Single();
        salvo.DataLancamento.Should().BeOnOrAfter(antes);
    }

    [Fact]
    public async Task Deve_Preservar_DataLancamento_Informada()
    {
        await using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var dataEsperada = DateTimeOffset.UtcNow.AddDays(-5);
        var command = LancamentoCommandFaker.ValidVenda() with { DataLancamento = dataEsperada };

        await handler.Handle(command);

        var salvo = db.Lancamentos.Single();
        salvo.DataLancamento.Should().BeCloseTo(dataEsperada, TimeSpan.FromSeconds(1));
    }

    // ── Outbox ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Deve_Enfileirar_OutboxMessage_Na_Mesma_Operacao()
    {
        await using var db = CreateDbContext();
        var handler = CreateHandler(db);

        await handler.Handle(LancamentoCommandFaker.ValidVenda(1000));

        db.OutboxMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task Deve_Enfileirar_OutboxMessage_Com_Exchange_E_RoutingKey_Corretos()
    {
        await using var db = CreateDbContext();
        var handler = CreateHandler(db);

        await handler.Handle(LancamentoCommandFaker.ValidVenda(1000));

        var msg = db.OutboxMessages.Single();
        msg.Exchange.Should().Be("lancamentos.events");
        msg.RoutingKey.Should().Be("lancamento.criado");
        msg.MessageType.Should().Be(nameof(SharedKernelEvents.LancamentoRegistradoMessage));
    }

    [Fact]
    public async Task Deve_Enfileirar_Payload_Com_Valor_Correto()
    {
        await using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = LancamentoCommandFaker.ValidVenda(3333);

        await handler.Handle(command);

        var payload = db.OutboxMessages.Single().Payload;
        var evento = JsonSerializer.Deserialize<SharedKernelEvents.LancamentoRegistradoMessage>(payload, JsonOptions)!;
        evento.Valor.Should().Be(3333);
    }

    [Fact]
    public async Task Deve_Enfileirar_Payload_Com_CorrelationId_Do_Comando()
    {
        await using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var command = LancamentoCommandFaker.ValidVenda();

        await handler.Handle(command);

        var payload = db.OutboxMessages.Single().Payload;
        var evento = JsonSerializer.Deserialize<SharedKernelEvents.LancamentoRegistradoMessage>(payload, JsonOptions)!;
        evento.CorrelationId.Should().Be(command.CorrelationId);
    }

    [Fact]
    public async Task OutboxMessage_Deve_Iniciar_Nao_Entregue()
    {
        await using var db = CreateDbContext();
        var handler = CreateHandler(db);

        await handler.Handle(LancamentoCommandFaker.ValidVenda());

        db.OutboxMessages.Single().DeliveredAt.Should().BeNull();
    }

    // ── Múltiplos lançamentos ────────────────────────────────────────────────

    [Fact]
    public async Task Deve_Persistir_Multiplos_Lancamentos_Independentes()
    {
        await using var db = CreateDbContext();
        var handler = CreateHandler(db);

        await handler.Handle(LancamentoCommandFaker.ValidVenda(1000));
        await handler.Handle(LancamentoCommandFaker.ValidDebito(ModalidadeLancamento.Devolucao, -500));
        await handler.Handle(LancamentoCommandFaker.ValidVenda(2500));

        db.Lancamentos.Should().HaveCount(3);
        db.OutboxMessages.Should().HaveCount(3);
    }
}
