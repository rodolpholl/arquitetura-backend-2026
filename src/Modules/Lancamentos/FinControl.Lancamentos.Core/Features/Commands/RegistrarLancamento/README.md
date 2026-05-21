# Registrar Lançamento - Vertical Slice

Implementação de uma funcionalidade completa de registro de lançamento usando **Vertical Slicing** com **Wolverine** e **FluentValidator**.

## 📋 Arquitetura

```
POST /lancamentos/registrar
    ↓
RegistrarLancamentoEndpoint (HTTP)
    ↓
Wolverine Bus (Mediator)
    ↓
RegistrarLancamentoCommandHandler (Business Logic)
    ↓
IRepository<Lancamento> (Data Access)
    ↓
PostgreSQL
```

## 📁 Estrutura de Arquivos

```
Features/Commands/RegistrarLancamento/
├── RegistrarLancamentoCommand.cs              # Command (mediator)
├── RegistrarLancamentoCommandValidator.cs     # Validação (FluentValidator)
├── RegistrarLancamentoCommandHandler.cs       # Handler (lógica de negócio)
├── RegistrarLancamentoEndpoint.cs             # Endpoint HTTP (Wolverine)
├── RegistrarLancamentoResponse.cs             # Response DTO
└── README.md                                  # Este arquivo
```

## 🚀 Como Usar

### 1. Integração no Program.cs

```csharp
// Program.cs do módulo Lancamentos.API
var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Services
    .AddLancamentosFeatures()  // Registra handlers, validators e endpoints
    .AddRepository<Lancamento>()  // Repositório genérico
    .AddWolverine();  // Wolverine como mediator

var app = builder.Build();
app.MapWolverineEndpoints();  // Mapeia endpoints Wolverine

app.Run();
```

### 2. Chamada HTTP

```bash
POST http://localhost:5000/lancamentos/registrar
Content-Type: application/json
Authorization: Bearer {token-keycloak}
x-correlation-id: 550e8400-e29b-41d4-a716-446655440000
idempotency-key: 6ba7b810-9dad-11d1-80b4-00c04fd430c8

{
  "modalidade": 1,
  "valor": 15050,
  "descricao": "Venda de produtos",
  "dataLancamento": "2026-05-21T10:30:00Z"
}
```

### 3. Resposta

```json
{
  "id": 1,
  "navigationId": "550e8400-e29b-41d4-a716-446655440001",
  "idempotencyKey": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "criadoEm": "2026-05-21T10:30:00.000Z"
}
```

## ✅ Fluxo de Execução

1. **Endpoint recebe request** → extrai usuário do contexto Keycloak
2. **Constrói Command** → injeta dados de auditoria (usuarioId, nome, email)
3. **Wolverine invoca Handler** → validação automática via FluentValidator
4. **Handler executa lógica** → verifica idempotência, cria entidade, persiste
5. **Response é retornada** → headers de rastreamento adicionados
6. **Evento pode ser publicado** → integração com Outbox/RabbitMQ

## 🔒 Validações

- ✅ Modalidade deve ser um enum válido
- ✅ Valor deve ser > 0 e ≤ R$ 999.999.999,99
- ✅ Descrição obrigatória se Modalidade = "Outros"
- ✅ Data não pode ser no futuro (máximo 1 dia à frente)
- ✅ Data não pode ser anterior a 1 ano
- ✅ Email deve ter formato válido
- ✅ IDs de rastreamento devem ser UUID válidos

## 🔄 Idempotência

A chave `idempotency-key` garante que múltiplas requisições com a mesma chave retornem o mesmo resultado:

```bash
# Primeira chamada - cria
POST /lancamentos/registrar
idempotency-key: abc-123

# Segunda chamada com mesma chave - retorna mesmo resultado
POST /lancamentos/registrar
idempotency-key: abc-123
```

## 📊 Rastreamento Distribuído

- **x-correlation-id** — rastreamento ponta-a-ponta (trace distribuído)
- **idempotency-key** — prevenção de duplicação
- **Propriedades de auditoria** — CreatedBy, CreatedByName, CreatedByEmail

## 🔌 Extensões Futuras

- [ ] Publicar evento de domínio → RabbitMQ/Outbox
- [ ] Integração com Consolidado → atualizar cache Redis
- [ ] Validação de regras de negócio contra módulo Consolidado
- [ ] Webhooks para sistemas externos
- [ ] Auditoria com Change Tracking completo

## 📚 Referências

- [Wolverine Documentation](https://wolverine.netlify.app/)
- [FluentValidation](https://fluentvalidation.net/)
- [Vertical Slicing in .NET](https://www.jimmybogard.com/vertical-slice-architecture/)
