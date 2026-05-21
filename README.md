# 🏗️ Agile Workers - Backend 2026

**Sistema de Controle de Fluxo de Caixa**  
Arquitetura Backend | ASP.NET Core 8 | Wolverine | PostgreSQL

---

## 📚 Documentação

Toda a documentação técnica está em [`.docs/`](.docs) — comece por aqui:

### 🎯 Primeiros Passos
- **[CONVENCOES.md](.docs/CONVENCOES.md)** ← Leia primeiro! Premissas do projeto
- **[INDEX.md](.docs/INDEX.md)** ← Índice e navegação

### 🏗️ Arquitetura
- **[ARQUITETURA.md](.docs/ARQUITETURA.md)** — Decisões tecnológicas, volumetria, stack
- **[PROGRESSO.md](.docs/PROGRESSO.md)** — Status, próximas fases, roadmap

### 🚀 Implementação
- **[GUIA_IMPLEMENTACAO_WOLVERINE.md](.docs/GUIA_IMPLEMENTACAO_WOLVERINE.md)** — Tutorial 4 semanas
- **[WOLVERINE_QUICK_REFERENCE.md](.docs/WOLVERINE_QUICK_REFERENCE.md)** — Exemplos de código

### 📋 Recursos
- **[TEMPLATE_NOVO_DOCUMENTO.md](.docs/TEMPLATE_NOVO_DOCUMENTO.md)** — Padrão para novos docs
- **[desafio-arquiteto-software.pdf](.docs/desafio-arquiteto-software.pdf)** — Requisitos iniciais

---

## ⚡ Quick Start

```bash
# 1. Infraestrutura
docker-compose -f DOCKER-COMPOSE.md up -d

# 2. Build
dotnet build

# 3. Testes
dotnet test

# 4. Run
dotnet run --project Lancamentos.API
```

---

## 🎯 Requisitos

- **Lançamentos**: Débitos/créditos com ACID e idempotência
- **Consolidado**: Saldo diário com 50 req/s (máximo 5% de perda)
- **Performance**: Cache Redis + CQRS + Event-Driven
- **Observabilidade**: Jaeger + Prometheus + Grafana

---

## 📊 Stack

```
Backend:        ASP.NET Core 10 + Wolverine 5.39
Database:       PostgreSQL 16 + Marten (Event Store)
Message Bus:    RabbitMQ 3.12
Cache:          Redis 7
Tracing:        Jaeger + OpenTelemetry
Metrics:        Prometheus
Secrets:        HashiCorp Vault
Pattern:        Vertical Slice + CQRS + Event-Driven
```

---

## 🔗 Links Úteis

- 📖 [Wolverine Docs](https://wolverine.netlify.app/)
- 📖 [Marten Docs](https://martendb.io/)
- 📖 [FluentValidation](https://docs.fluentvalidation.net/)

---

**Status:** ✅ Documentação Centralizada | 📋 Infraestrutura Pronta | 🚀 Pronto para Implementação

**Próximo passo:** Leia [CONVENCOES.md](.docs/CONVENCOES.md) e depois [GUIA_IMPLEMENTACAO_WOLVERINE.md](.docs/GUIA_IMPLEMENTACAO_WOLVERINE.md)
