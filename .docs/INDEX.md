# 📚 Documentação - Agile Workers Backend 2026

> **⚠️ Premissa Arquitetural:** Toda documentação do projeto deve estar centralizada nesta pasta `.docs/`. Novos documentos sempre serão criados aqui, nunca na raiz do repositório.

---

## 📋 Índice de Documentação

### � Primeiros Passos

| Documento | Propósito | Status |
|-----------|-----------|--------|
| [CONVENCOES.md](CONVENCOES.md) | **🚀 Leia primeiro!** Premissas e convenções obrigatórias para documentação | ✅ Ativo |
| [TEMPLATE_NOVO_DOCUMENTO.md](TEMPLATE_NOVO_DOCUMENTO.md) | Template padrão para criar nova documentação (copie e adapte) | 📄 Template |

### �🏗️ Arquitetura & Design

| Documento | Propósito | Status |
|-----------|-----------|--------|
| [ARQUITETURA.md](ARQUITETURA.md) | Planejamento arquitetural completo, requisitos, decisões tecnológicas e análise de volumetria | ✅ Ativo |
| [GUIA_IMPLEMENTACAO_WOLVERINE.md](GUIA_IMPLEMENTACAO_WOLVERINE.md) | Guia passo-a-passo para implementação com Wolverine em 4 semanas | ✅ Ativo |
| [WOLVERINE_QUICK_REFERENCE.md](WOLVERINE_QUICK_REFERENCE.md) | Referência rápida do framework Wolverine (mediator + message bus) | ✅ Ativo |
| [desafio-arquiteto-software.pdf](desafio-arquiteto-software.pdf) | Desafio original do arquiteto de software (requisitos iniciais) | 📎 Referência |

---

## 🎯 Guias Rápidos

### Para Iniciantes
1. **Leia primeiro:** [CONVENCOES.md](CONVENCOES.md) - Premissas fundamentais
2. Depois: [ARQUITETURA.md](ARQUITETURA.md) - seção **Resumo Executivo**
3. Consulte: [WOLVERINE_QUICK_REFERENCE.md](WOLVERINE_QUICK_REFERENCE.md) - seção **Setup & Installation**
4. Siga: [GUIA_IMPLEMENTACAO_WOLVERINE.md](GUIA_IMPLEMENTACAO_WOLVERINE.md) - **Fase 1: Setup Inicial**

### Para Implementadores
1. Stack técnico: veja [ARQUITETURA.md](ARQUITETURA.md) - seção **Stack Técnico**
2. Padrões aplicados: [ARQUITETURA.md](ARQUITETURA.md) - seção **Padrões Arquiteturais Aplicados**
3. Exemplos práticos: [WOLVERINE_QUICK_REFERENCE.md](WOLVERINE_QUICK_REFERENCE.md) - todas as seções

### Para Arquitetos
1. Decisões tecnológicas: [ARQUITETURA.md](ARQUITETURA.md) - seção **Justificativas Tecnológicas**
2. Análise de volumetria: [ARQUITETURA.md](ARQUITETURA.md) - seção **Análise de Volumetria**
3. Plano de implementação: [ARQUITETURA.md](ARQUITETURA.md) - seção **Plano de Implementação**

---

## 📐 Stack Técnico Resumido

```
Backend:        ASP.NET Core 8 + Wolverine 5.39
Message Bus:    RabbitMQ 3.12
Database:       PostgreSQL 16
Cache:          Redis 7
Event Store:    Marten (PostgreSQL)
API Gateway:    Kong
Tracing:        Jaeger + OpenTelemetry
Metrics:        Prometheus
Dashboards:     Grafana
Secrets:        HashiCorp Vault
Pattern:        Vertical Slice + CQRS + Event-Driven
```

---

## 🔄 Fluxo de Lançamentos

```
Cliente HTTP
    ↓
ASP.NET Core Minimal API
    ↓
Wolverine Handler (Command)
    ├─ Validação (FluentValidation)
    ├─ Lógica de Negócio (Domain)
    └─ Salva Evento + Outbox (Marten)
    ↓
RabbitMQ (evento publicado)
    ↓
Event Handlers
    ├─ Atualiza cache Redis
    ├─ Publica para consolidado
    └─ Registra logs/traces
```

---

## 🚀 Roadmap Implementação

### Semana 1: Setup Inicial
- [x] Criar estrutura de projetos
- [x] Configurar Wolverine + Marten
- [x] Implementar Docker Compose

### Semana 2: Handlers & CQRS
- [ ] Implementar handlers de comando (lançamentos)
- [ ] Implementar queries (consolidado)
- [ ] Testes unitários

### Semana 3: Integração & Eventos
- [ ] RabbitMQ integration
- [ ] Outbox pattern
- [ ] Testes de integração

### Semana 4: Observabilidade & Deploy
- [ ] OpenTelemetry setup
- [ ] Prometheus + Grafana
- [ ] Deploy inicial

---

## 📞 Referências Rápidas

### Documentos Relacionados
- 🔗 [README.md](../README.md) - Overview geral do projeto
- 🔗 [DOCKER-COMPOSE.md](../DOCKER-COMPOSE.md) - Infraestrutura local (Docker)
- 🔗 [Makefile](../Makefile) - Comandos úteis

### Frameworks & Bibliotecas
- 📖 [Wolverine Docs](https://wolverine.netlify.app/) - Documentação oficial
- 📖 [Marten Docs](https://martendb.io/) - Event store
- 📖 [FluentValidation](https://docs.fluentvalidation.net/) - Validação

### Padrões Aplicados
- **Vertical Slice Architecture** - Organização por feature
- **CQRS** - Separação Command/Query
- **Event-Driven** - Comunicação via eventos
- **Outbox Pattern** - Garantia de entrega

---

## ⚡ Comandos Úteis

```bash
# Iniciar infraestrutura
docker-compose up -d

# Build projeto
dotnet build

# Executar testes
dotnet test

# Run aplicação
dotnet run --project Lancamentos.API

# Ver logs
docker-compose logs -f rabbitmq
docker-compose logs -f postgres
```

---

## 📝 Adicionar Novo Documento

Quando precisar criar novo documento:

1. **Sempre na pasta `.docs/`** (nunca na raiz)
2. Use o formato **NOME_DO_DOCUMENTO.md** (PascalCase)
3. Atualize este INDEX.md com referência ao novo documento
4. Mantenha consistência com documentação existente

### Template para Novo Documento

```markdown
# 📄 Título do Documento

> **Objetivo:** Uma linha descrevendo o propósito

---

## 📋 Índice

- [Seção 1](#seção-1)
- [Seção 2](#seção-2)

---

## Seção 1

Conteúdo...

## Seção 2

Conteúdo...

---

**Última atualização:** Maio 2026  
**Versão:** 1.0
```

---

## ✅ Checklist de Documentação

- [ ] Documento em `.docs/`
- [ ] Título com emoji apropriado
- [ ] Objetivo claro no início
- [ ] Índice de seções
- [ ] Exemplos práticos
- [ ] Links internos funcionando
- [ ] Data de atualização informada
- [ ] INDEX.md atualizado

---

**Última atualização:** Maio 2026  
**Centralizado em:** `.docs/`  
**Status:** ✅ Production-Ready
