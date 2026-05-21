# 🚀 Guia de Implementação: Wolverine para Agile Workers

**Objetivo:** Implementar sistema de lançamentos financeiro com Wolverine em 4 semanas  
**Stack:** Wolverine + Marten + RabbitMQ + PostgreSQL + Kong  
**Padrão:** Vertical Slice + CQRS

---

## 📦 Fase 1: Setup Inicial (Semana 1 - 1 dia)

### 1.1 Criar Solução

```bash
# Criar diretório
mkdir AgileWorkers.Lancamentos
cd AgileWorkers.Lancamentos

# Criar solução
dotnet new sln -n AgileWorkers.Lancamentos

# Criar projetos
dotnet new globaljson --sdk-version 8.0.0 --roll-forward latestFeature

# API principal
dotnet new webapi -n Lancamentos.API -f net8.0
dotnet sln add Lancamentos.API/Lancamentos.API.csproj

# Testes
dotnet new xunit -n Lancamentos.Tests -f net8.0
dotnet sln add Lancamentos.Tests/Lancamentos.Tests.csproj

# Shared domain
dotnet new classlib -n Lancamentos.Domain -f net8.0
dotnet sln add Lancamentos.Domain/Lancamentos.Domain.csproj
```

### 1.2 Adicionar NuGet Packages

```bash
cd Lancamentos.API

# Wolverine
dotnet add package WolverineFx -v 5.39.1
dotnet add package WolverineFx.RabbitMQ -v 5.39.1
dotnet add package WolverineFx.Marten -v 5.39.1

# Marten (PostgreSQL event store)
dotnet add package Marten -v 8.0.0

# OpenTelemetry
dotnet add package OpenTelemetry -v 1.8.0
dotnet add package OpenTelemetry.Exporter.Console -v 1.8.0
dotnet add package OpenTelemetry.Instrumentation.AspNetCore -v 1.8.0

# Logging
dotnet add package Serilog.AspNetCore -v 8.0.0
dotnet add package Serilog.Sinks.Console -v 5.0.0

# Validation
dotnet add package FluentValidation -v 11.9.0
dotnet add package FluentValidation.DependencyInjectionExtensions -v 11.9.0

# Testing
cd ../Lancamentos.Tests
dotnet add package Wolverine -v 5.39.1
dotnet add package xunit.runner.visualstudio -v 2.7.0
dotnet add package Microsoft.NET.Test.Sdk -v 17.9.0
dotnet add package Testcontainers -v 3.7.0
dotnet add package Testcontainers.PostgreSQL -v 3.7.0
dotnet add package Testcontainers.RabbitMQ -v 3.7.0
```

### 1.3 Docker Compose

**Arquivo:** `docker-compose.yml`

```yaml
version: '3.8'

services:
  # PostgreSQL para Wolverine Outbox + Marten
  postgres:
    image: postgres:15-alpine
    container_name: lancamentos-postgres
    environment:
      POSTGRES_DB: lancamentos
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  # RabbitMQ para messaging
  rabbitmq:
    image: rabbitmq:3.12-management-alpine
    container_name: lancamentos-rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: admin
    ports:
      - "5672:5672"      # AMQP
      - "15672:15672"    # Management UI
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  postgres_data:
  rabbitmq_data:
```

**Iniciar:**
```bash
docker-compose up -d
```

---

## 🏗️ Fase 2: Estrutura de Pastas (Vertical Slice)

### 2.1 Criar Estrutura

```bash
cd Lancamentos.API

# Features
mkdir -p Features/RegistrarDebito
mkdir -p Features/RegistrarCredito
mkdir -p Features/ConsultarLancamentos

# Shared
mkdir -p Shared/Domain/Entities
mkdir -p Shared/Domain/Events
mkdir -p Shared/Domain/ValueObjects
mkdir -p Shared/Infrastructure/Data
mkdir -p Shared/Infrastructure/Events
mkdir -p Shared/Behaviors
mkdir -p Shared/Exceptions
mkdir -p Shared/Extensions
```

### 2.2 Domain Entities

**Arquivo:** `Shared/Domain/Entities/Lancamento.cs`

```csharp
namespace Lancamentos.API.Shared.Domain.Entities;

public class Lancamento
{
    public Guid Id { get; set; }
    public TipoLancamento Tipo { get; set; }
    public decimal Valor { get; set; }
    public string Descricao { get; set; }
    public DateTime RegistradoEm { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }

    public Lancamento() { }

    public Lancamento(
        Guid id,
        TipoLancamento tipo,
        decimal valor,
        string descricao,
        DateTime registradoEm)
    {
        Id = id;
        Tipo = tipo;
        Valor = valor;
        Descricao = descricao;
        RegistradoEm = registradoEm;
        Ativo = true;
        CriadoEm = DateTime.UtcNow;
    }
}

public enum TipoLancamento
{
    Debito = 0,
    Credito = 1
}
```

**Arquivo:** `Shared/Domain/ValueObjects/Valor.cs`

```csharp
namespace Lancamentos.API.Shared.Domain.ValueObjects;

public record Valor(decimal Montante)
{
    public static Valor Zero() => new(0m);
    
    public Valor Plus(Valor other) => new(Montante + other.Montante);
    
    public Valor Minus(Valor other) => new(Montante - other.Montante);
}
```

### 2.3 Domain Events

**Arquivo:** `Shared/Domain/Events/LancamentoRegistradoEvent.cs`

```csharp
namespace Lancamentos.API.Shared.Domain.Events;

public class LancamentoRegistradoEvent
{
    public Guid LancamentoId { get; set; }
    public decimal Valor { get; set; }
    public string Tipo { get; set; } // "D" ou "C"
    public DateTime RegistradoEm { get; set; }
    public DateTime EventoEm { get; set; } = DateTime.UtcNow;
}
```

### 2.4 Marten Configuration

**Arquivo:** `Shared/Infrastructure/Data/MartenConfiguration.cs`

```csharp
namespace Lancamentos.API.Shared.Infrastructure.Data;

using Lancamentos.API.Shared.Domain.Entities;
using Marten;
using Wolverine;

public static class MartenConfiguration
{
    public static IServiceCollection AddMartenStore(
        this IServiceCollection services,
        IWebHostEnvironment env)
    {
        var connectionString = env.IsDevelopment()
            ? "host=localhost;database=lancamentos;username=postgres;password=postgres"
            : Environment.GetEnvironmentVariable("DATABASE_URL") 
              ?? throw new InvalidOperationException("DATABASE_URL not set");

        services.AddMarten(opts =>
        {
            opts.Connection(connectionString);
            
            // Documento store
            opts.Schema.For<Lancamento>().Identity(x => x.Id);
            
            // Wolverine Outbox integration
            opts.Integrations.Add(new WolverineIntegration());
        })
        .UseLightweightSessions()
        .ApplyAllDatabaseChangesOnStartup();

        return services;
    }
}
```

---

## 💻 Fase 3: Feature - Registrar Débito

### 3.1 Command

**Arquivo:** `Features/RegistrarDebito/RegistrarDebitoCommand.cs`

```csharp
namespace Lancamentos.API.Features.RegistrarDebito;

public record RegistrarDebitoCommand(
    decimal Valor,
    string Descricao
);

public record DebitoResponse(
    Guid Id,
    decimal Valor,
    DateTime RegistradoEm,
    string Status
);
```

### 3.2 Validator

**Arquivo:** `Features/RegistrarDebito/RegistrarDebitoValidator.cs`

```csharp
namespace Lancamentos.API.Features.RegistrarDebito;

using FluentValidation;

public class RegistrarDebitoValidator : AbstractValidator<RegistrarDebitoCommand>
{
    public RegistrarDebitoValidator()
    {
        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("Valor deve ser maior que zero")
            .LessThanOrEqualTo(1000000).WithMessage("Valor não pode exceder 1.000.000");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição é obrigatória")
            .MaximumLength(500).WithMessage("Descrição máximo 500 caracteres");
    }
}
```

### 3.3 Handler

**Arquivo:** `Features/RegistrarDebito/RegistrarDebitoHandler.cs`

```csharp
namespace Lancamentos.API.Features.RegistrarDebito;

using Lancamentos.API.Shared.Domain.Entities;
using Lancamentos.API.Shared.Domain.Events;
using Marten;
using Wolverine;
using Microsoft.Extensions.Logging;

public class RegistrarDebitoHandler
{
    private readonly ILogger<RegistrarDebitoHandler> _logger;

    public RegistrarDebitoHandler(ILogger<RegistrarDebitoHandler> logger)
    {
        _logger = logger;
    }

    public async Task<DebitoResponse> Handle(
        RegistrarDebitoCommand command,
        IMessageContext context,
        IDocumentSession session,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Registrando débito: {Valor:C} - {Descricao}",
            command.Valor,
            command.Descricao);

        // 1. Criar entidade
        var lancamento = new Lancamento(
            id: Guid.NewGuid(),
            tipo: TipoLancamento.Debito,
            valor: command.Valor,
            descricao: command.Descricao,
            registradoEm: DateTime.UtcNow);

        // 2. Persistir (transação Marten)
        session.Store(lancamento);

        // 3. ✅ Publicar evento COM OUTBOX AUTOMÁTICO!
        // Wolverine gerencia Inbox/Outbox na mesma transação
        await context.PublishAsync(
            new LancamentoRegistradoEvent
            {
                LancamentoId = lancamento.Id,
                Valor = lancamento.Valor,
                Tipo = "D",
                RegistradoEm = lancamento.RegistradoEm
            },
            outbox: true,
            cancellation: ct);

        // 4. Salvar tudo atomicamente
        await session.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Débito registrado: {LancamentoId}",
            lancamento.Id);

        return new DebitoResponse(
            lancamento.Id,
            lancamento.Valor,
            lancamento.RegistradoEm,
            "Registrado");
    }
}
```

### 3.4 Endpoint (Minimal API)

**Arquivo:** `Features/RegistrarDebito/RegistrarDebitoEndpoint.cs`

```csharp
namespace Lancamentos.API.Features.RegistrarDebito;

using Wolverine;

public static class RegistrarDebitoEndpoint
{
    public static void MapRegistrarDebito(this WebApplication app)
    {
        app.MapPost("/api/lancamentos/debito", Handle)
            .WithName("RegistrarDebito")
            .WithOpenApi()
            .Produces<DebitoResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> Handle(
        RegistrarDebitoCommand request,
        IMessageBus mediator,
        CancellationToken ct)
    {
        var response = await mediator.InvokeAsync<DebitoResponse>(request, ct);
        return Results.Created($"/api/lancamentos/{response.Id}", response);
    }
}
```

### 3.5 Tests

**Arquivo:** `Lancamentos.Tests/Features/RegistrarDebito/RegistrarDebitoHandlerTests.cs`

```csharp
namespace Lancamentos.Tests.Features.RegistrarDebito;

using Lancamentos.API.Features.RegistrarDebito;
using Lancamentos.API.Shared.Domain.Entities;
using Marten;
using Wolverine;
using Xunit;

public class RegistrarDebitoHandlerTests
{
    [Fact]
    public async Task Handle_ComValorValido_RegistraDebito()
    {
        // Arrange
        var command = new RegistrarDebitoCommand(
            Valor: 100m,
            Descricao: "Venda produto X");

        // Mock documentSession
        var mockSession = new MockDocumentSession();
        var mockContext = new MockMessageContext();
        var handler = new RegistrarDebitoHandler(new MockLogger());

        // Act
        var result = await handler.Handle(
            command,
            mockContext,
            mockSession,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100m, result.Valor);
        Assert.Equal("Registrado", result.Status);

        // Verificar evento foi publicado
        Assert.True(mockContext.EventosPublicados.Any());
    }
}
```

---

## 🐰 Fase 4: Program.cs (Wolverine Setup)

**Arquivo:** `Program.cs`

```csharp
using Lancamentos.API.Shared.Infrastructure.Data;
using Wolverine;
using Wolverine.Marten;
using Wolverine.RabbitMq;
using Serilog;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Logging
    builder.Host.UseSerilog();

    // Wolverine + Marten + RabbitMQ
    builder.Host.UseWolverine((context, opts) =>
    {
        opts.Services.AddFluentValidationAutoValidation();

        // 1. Database: Marten com Outbox
        opts.Services.AddMartenStore(context.HostingEnvironment);

        // 2. Message Bus: RabbitMQ
        opts.UseRabbitMq(opts =>
        {
            opts.HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
            opts.AutoProvision();
            opts.UseDurableOutbox(); // ✅ Outbox em PostgreSQL
        });

        // 3. Handler Discovery
        opts.IncludeAssemblyContaining<Program>();

        // 4. OpenTelemetry
        opts.UseOpenTelemetry()
            .WithTracing()
            .WithMetrics();

        // 5. Policies (resilience)
        opts.Policies
            .OnException<InvalidOperationException>()
            .Retry(attempts: 3, delay: TimeSpan.FromSeconds(1))
            .AndThen(ContinueAction.MoveToErrorQueue);
    });

    // Serviços
    builder.Services.AddScoped<FluentValidation.IValidatorFactory>(
        sp => new FluentValidation.ValidatorFactory());

    // Endpoints
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // OpenTelemetry Prometheus endpoint
    app.UseOpenTelemetryPrometheusScrapingEndpoint();

    // Swagger
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Endpoints
    app.MapPost("/api/lancamentos/debito", HandleRegistrarDebito)
        .WithName("RegistrarDebito")
        .WithOpenApi();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação encerrou com erro");
}
finally
{
    Log.CloseAndFlush();
}

// Endpoint handler
static async Task<IResult> HandleRegistrarDebito(
    RegistrarDebitoCommand request,
    IMessageBus bus,
    CancellationToken ct)
{
    var response = await bus.InvokeAsync<DebitoResponse>(request, ct);
    return Results.Created($"/api/lancamentos/{response.Id}", response);
}
```

---

## ✅ Fase 5: Validação

### 5.1 Testar Localmente

```bash
# Terminal 1: Docker services
docker-compose up -d

# Terminal 2: API
cd Lancamentos.API
dotnet run

# Terminal 3: Registrar débito
curl -X POST http://localhost:5000/api/lancamentos/debito \
  -H "Content-Type: application/json" \
  -d '{"valor": 100.00, "descricao": "Venda produto X"}'

# Response:
# {
#   "id": "uuid...",
#   "valor": 100.00,
#   "registradoEm": "2026-05-20T...",
#   "status": "Registrado"
# }

# Verificar RabbitMQ
# http://localhost:15672 (admin/admin)
# Confirmar fila "lancamentos.registradoevent" criada

# Verificar PostgreSQL Outbox
psql -h localhost -U postgres -d lancamentos
SELECT * FROM wolverine_outbox;
# Deve mostrar evento persistido
```

### 5.2 Testar Outbox

```csharp
[Fact]
public async Task Outbox_EventoPersisteEmFalha()
{
    // Dado: handler que falha após publicar evento
    var command = new RegistrarDebitoCommand(100m, "Teste");
    
    // Quando: handler executa
    // Então: evento persiste em outbox MESMO com erro posterior
    
    // Verification:
    // 1. Evento em lancamentos.lancamento
    // 2. Evento em wolverine_outbox
    // 3. Retry automático dispara
    // 4. Evento entregue em RabbitMQ
}
```

---

## 📊 Fase 6: Consumer (Consolidado Service)

**Arquivo:** `Shared/Infrastructure/Events/LancamentoEventConsumer.cs`

```csharp
namespace Lancamentos.API.Shared.Infrastructure.Events;

using Lancamentos.API.Shared.Domain.Events;
using Microsoft.Extensions.Logging;

public class LancamentoEventConsumer
{
    private readonly ILogger<LancamentoEventConsumer> _logger;

    public LancamentoEventConsumer(ILogger<LancamentoEventConsumer> logger)
    {
        _logger = logger;
    }

    // ✅ Wolverine auto-descobre este handler
    public async Task Handle(LancamentoRegistradoEvent evento, CancellationToken ct)
    {
        _logger.LogInformation(
            "Evento recebido: {LancamentoId} - {Valor:C} ({Tipo})",
            evento.LancamentoId,
            evento.Valor,
            evento.Tipo);

        // Atualizar consolidado, cache, etc
        await Task.Delay(100, ct); // Simular trabalho
        
        _logger.LogInformation("Evento processado: {LancamentoId}", evento.LancamentoId);
    }
}
```

---

## 🚀 Próximas Etapas

1. **Adicione mais Features:** RegistrarCredito, ConsultarLancamentos
2. **Implemente Consolidado Service:** Consumer que agrega dados
3. **Cache com Redis:** Para queries rápidas
4. **Kong API Gateway:** Rate limiting, circuit breaker
5. **Deploy:** Kubernetes com Wolverine + Marten

---

## 📚 Recursos

- **Docs Wolverine:** https://wolverine.netlify.app
- **Marten Docs:** https://martendb.io
- **Docker Compose:** Services prontos
- **RabbitMQ UI:** http://localhost:15672

**Total de tempo de implementação:** ~4h  
**Resultado:** Sistema robusto com Outbox Pattern built-in ✅

