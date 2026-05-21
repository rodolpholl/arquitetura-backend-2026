# Arquitetura de Endpoints - FinControl.Lancamentos.API

## 📋 Visão Geral

Este projeto é a **porta de entrada (API)** do módulo Lancamentos. Utiliza:

- **Wolverine** como framework para minimal APIs com auto-descoberta de endpoints
- **FluentValidation** para validação automática
- **OpenTelemetry + Prometheus** para observabilidade
- **Serilog + Grafana Loki** para logging estruturado
- **HashiCorp Vault** para gerenciamento de secrets
- **Health Checks** para liveness/readiness probes

### Estrutura de Dependências

```
Program.cs (API)
  ├─ AddFinControlVault()
  │   └─ Carrega secrets do Vault
  ├─ AddFinControlSerilog()
  │   └─ Configura logging → Console + File + Loki
  ├─ AddFinControlObservability()
  │   └─ OpenTelemetry Traces → Jaeger / Tempo
  │   └─ Prometheus Metrics → /metrics
  ├─ AddDbContext<LancamentosDbContext>()
  │   └─ EF Core + PostgreSQL
  ├─ AddFinControlHealthChecks()
  │   └─ PostgreSQL, Redis, RabbitMQ health checks
  ├─ AddAllModules()
  │   └─ Registra Wolverine + todos os handlers
  └─ Exception Handler Global + ProblemDetails
```

---

## ✅ Como Funciona a Auto-Descoberta

### 1️⃣ Criar um Endpoint

No módulo `FinControl.Lancamentos.Core`, crie uma classe endpoint com método `Handle`:

```csharp
namespace FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;

public class RegistrarLancamentoEndpoint
{
    [WolverinePost("/api/lancamentos/registrar")]
    public async Task<RegistrarLancamentoResponse> Handle(
        RegistrarLancamentoRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken = default)
    {
        // 1. Extrair dados do JWT
        var (usuarioId, usuarioNome, usuarioEmail) = httpContext.ExtrairDadosUsuario();
        var correlationId = httpContext.ExtrairCorrelationId();
        var idempotencyKey = httpContext.ExtrairIdempotencyKey();

        // 2. Construir comando
        var command = new RegistrarLancamentoCommand { ... };

        // 3. Executar via Wolverine Bus
        var response = await bus.InvokeAsync<RegistrarLancamentoResponse>(command, cancellationToken);

        // 4. Adicionar headers de rastreamento
        httpContext.AdicionarHeadersRastreamento(correlationId, idempotencyKey);

        return response;
    }
}
```

**Atributos Wolverine disponíveis:**
- `[WolverineGet("/rota")]` — GET
- `[WolverinePost("/rota")]` — POST
- `[WolverinePut("/rota")]` — PUT
- `[WolverineDelete("/rota")]` — DELETE
- `[WolverinePatch("/rota")]` — PATCH

### 2️⃣ Criar um Handler

O método `Handle` é descoberto e executado automaticamente:

```csharp
namespace FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;

[Transactional]  // Wolverine envuelva a transação EF Core + Outbox
public class RegistrarLancamentoCommandHandler
{
    public async Task<RegistrarLancamentoResponse> Handle(
        RegistrarLancamentoCommand command,
        LancamentosDbContext context,
        IRepository<Lancamento> repository,
        CancellationToken cancellationToken = default)
    {
        // Lógica de negócio
        var lancamento = new Lancamento { ... };
        await repository.AddAsync(lancamento, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new RegistrarLancamentoResponse { ... };
    }
}
```

### 3️⃣ Criar um Validator

Validators com `FluentValidation` são descobertos e registrados automaticamente:

```csharp
public class RegistrarLancamentoCommandValidator : AbstractValidator<RegistrarLancamentoCommand>
{
    public RegistrarLancamentoCommandValidator()
    {
        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("Valor deve ser positivo");

        RuleFor(x => x.Modalidade)
            .IsInEnum().WithMessage("Modalidade inválida");
    }
}
```

**Fluxo de validação:**
1. Request chega no endpoint
2. Wolverine valida via `FluentValidationMiddleware`
3. Se falhar → `ValidationException` → HTTP 400 com erros por campo
4. Se passar → Handler é executado

---

## 🎯 Adicionar um Novo Módulo

### Passo 1: Criar a Estrutura

```
src/Modules/MeuModulo/
├── FinControl.MeuModulo.API/
│   ├── Program.cs (ou parte dele)
│   ├── ModuleExtensions.cs
│   └── Configuration/
│       └── ApplicationModules.cs (centralizador)
└── FinControl.MeuModulo.Core/
    ├── Context/
    │   └── MeuModuloDbContext.cs
    └── Features/
        ├── MeuModuloFeatureExtensions.cs
        ├── Commands/
        └── Queries/
```

### Passo 2: Implementar `MeuModuloFeatureExtensions.cs`

```csharp
namespace FinControl.MeuModulo.Core.Features;

public static class MeuModuloFeatureExtensions
{
    public static WebApplicationBuilder AddMeuModuloFeatures(
        this WebApplicationBuilder builder)
    {
        // Registrar Wolverine com auto-descoberta de handlers
        builder.AddFinControlWolverine<MeuModuloDbContext>(
            configure: null,
            typeof(SeuCommandHandler).Assembly
        );

        // Registrar validators
        builder.Services.AddValidatorsFromAssemblyContaining<SeuCommandValidator>(
            lifetime: ServiceLifetime.Transient);

        return builder;
    }

    public static WebApplication MapMeuModuloMiddleware(this WebApplication app)
    {
        app.UseFinControlMiddleware();
        return app;
    }
}
```

### Passo 3: Implementar `ModuleExtensions.cs` (no .API)

```csharp
namespace FinControl.MeuModulo.API;

public static class ModuleExtensions
{
    public static WebApplicationBuilder AddMeuModuloModule(
        this WebApplicationBuilder builder)
    {
        builder.AddMeuModuloFeatures();
        return builder;
    }

    public static WebApplication MapMeuModuloModule(this WebApplication app)
    {
        app.MapMeuModuloMiddleware();
        return app;
    }
}
```

### Passo 4: Registrar em `ApplicationModules.cs`

```csharp
public static class ApplicationModules
{
    public static WebApplicationBuilder AddAllModules(this WebApplicationBuilder builder)
    {
        builder.AddLancamentosModule();
        builder.AddMeuModuloModule();  // ✅ Novo módulo
        return builder;
    }

    public static WebApplication MapAllModules(this WebApplication app)
    {
        app.MapLancamentosModule();
        app.MapMeuModuloModule();  // ✅ Novo módulo
        return app;
    }
}
```

### Passo 5: Pronto! 🎉

Todos os endpoints, handlers e validators são descobertos **automaticamente**. Nenhum registro manual necessário além de adicionar as duas linhas em `ApplicationModules.cs`.

---

## 📊 Estrutura de Vertical Slice

Cada funcionalidade segue este padrão:

```
Features/
├── Commands/
│   └── RegistrarLancamento/
│       ├── RegistrarLancamentoCommand.cs         (Domain Model)
│       ├── RegistrarLancamentoCommandValidator.cs (Validation)
│       ├── RegistrarLancamentoCommandHandler.cs  (Business Logic)
│       ├── RegistrarLancamentoEndpoint.cs        (HTTP Layer)
│       ├── RegistrarLancamentoRequest.cs         (Input DTO)
│       ├── RegistrarLancamentoResponse.cs        (Output DTO)
│       └── README.md                             (Documentação)
└── Queries/
    └── ListarLancamentos/
        ├── ListarLancamentosQuery.cs
        ├── ListarLancamentosQueryHandler.cs
        ├── ListarLancamentosEndpoint.cs
        └── README.md
```

---

## 🔄 Fluxo de Requisição

```
HTTP Request (POST /api/lancamentos/registrar)
    ↓
Wolverine descobre [WolverinePost] → routing
    ↓
Injeção de dependências (HttpContext, IMessageBus, etc.)
    ↓
Extração de JWT (usuarioId, correlationId, etc.)
    ↓
Validação automática via FluentValidationMiddleware
    ├─ ❌ Falha → ValidationException → HTTP 400
    └─ ✅ Passa
        ↓
        Handler Handle() é executado
        ↓
        SaveChangesAsync() dispara Outbox + RabbitMQ
        ↓
        Response retorna
        ↓
HTTP Response 200 com ProblemDetails se erro
```

---

## 📝 Infrastructure: Middlewares Registrados

### 🔗 Vault
- **Função:** Carrega secrets do HashiCorp Vault
- **Chaves:** `postgres:connection_string`, `redis:connection_string`, `rabbitmq:uri`, `grafana:loki_url`, `grafana:otlp_endpoint`
- **Registro:** `builder.AddFinControlVault()` (PRIMEIRO)

### 📊 Serilog
- **Função:** Logging estruturado → Console + File (rotação diária) + Grafana Loki
- **Enriquecimento:** CorrelationId, MachineName, ThreadId, TraceId
- **Registro:** `builder.AddFinControlSerilog("fincontrol-lancamentos")`
- **Middleware:** `app.UseFinControlRequestLogging()`

### 🔍 Observabilidade
- **OpenTelemetry Traces:** Exporta spans via OTLP → Jaeger / Grafana Tempo
- **Prometheus Metrics:** Expõe `/metrics` para scraping
- **Registro:** `builder.AddFinControlObservability("fincontrol-lancamentos")`
- **Middleware:** `app.UseFinControlObservability()`

### ❤️ Health Checks
- **Função:** Liveness (`/health`) e Readiness (`/health/ready`)
- **Verifica:** PostgreSQL, Redis, RabbitMQ
- **Registro:** `builder.Services.AddFinControlHealthChecks(builder.Configuration)`

### ⚙️ Wolverine
- **Função:** Mediator + Bus + Outbox nativo
- **Middlewares:** LoggingMiddleware (antes) + FluentValidationMiddleware (depois)
- **Auto-descoberta:** Handlers por convenção (método `Handle/HandleAsync`)
- **Registro:** Feito em `LancamentosFeatureExtensions.AddLancamentosFeatures()`

### 🚨 Exception Handler Global
- **Função:** Mapeia exceções → ProblemDetails (RFC 7807)
- **Mapeamentos:**
  - `ValidationException` → HTTP 400
  - `ArgumentException` → HTTP 400
  - `UnauthorizedAccessException` → HTTP 401
  - `KeyNotFoundException` → HTTP 404
  - `InvalidOperationException` → HTTP 422
  - Demais → HTTP 500
- **Preserva:** CorrelationId em todas as respostas
- **Registro:** `builder.Services.AddExceptionHandler<GlobalExceptionHandler>()`

---

## ✨ Benefícios

✅ **Zero registro manual de endpoints** — Wolverine descobre tudo  
✅ **Escalável** — Adicione novos módulos sem tocar em Program.cs  
✅ **Type-safe** — Validação forte via FluentValidation  
✅ **Observável** — Traces, métricas, logs estruturados  
✅ **Auditável** — CorrelationId em toda requisição  
✅ **Resiliente** — Exception handler global + health checks  
✅ **Testável** — Cada slice é independente  

---

## 🛠️ Comandos Úteis

```bash
# Verificar endpoints descobertos
dotnet run

# Testar endpoint com JWT
curl -X POST https://localhost:5001/api/lancamentos/registrar \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{"modalidade": 1, "valor": 10000}'

# Ver logs
tail -f logs/fincontrol-lancamentos-*.log

# Health checks
curl https://localhost:5001/health
curl https://localhost:5001/health/ready

# Métricas Prometheus
curl https://localhost:5001/metrics
```

---

## 📚 Referências

- [Wolverine Docs](https://wolverine.readthedocs.io/)
- [FluentValidation](https://fluentvalidation.net/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Serilog](https://serilog.net/)
- [HashiCorp Vault](https://www.vaultproject.io/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)

