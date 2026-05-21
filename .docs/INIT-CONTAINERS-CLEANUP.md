# 🚀 Fluxo de Inicialização - Agile Workers Backend

## 📋 Visão Geral

A infraestrutura utiliza **containers de inicialização** (`vault-init` e `kong-init`) que executam scripts de configuração e então encerram. Esses containers **não devem permanecer em execução** e podem ser removidos após conclusão bem-sucedida.

---

## 🔄 Sequência de Inicialização

```
docker-compose up -d
        ↓
┌─────────────────────────────────────┐
│ FASE 1: Serviços de Infraestrutura  │
└─────────────────────────────────────┘
        ↓
  ✓ PostgreSQL (5-10s)
  ✓ Redis (3-5s)
  ✓ RabbitMQ (5-10s)
  ✓ Prometheus (10-15s)
  ✓ Jaeger (5-10s)
  ✓ Vault (15-20s)
        ↓
┌─────────────────────────────────────┐
│ FASE 2: Serviços de Aplicação       │
└─────────────────────────────────────┘
        ↓
  ✓ Keycloak (20-30s)
  ✓ Kong (15-25s)
  ✓ Grafana (quando Prometheus ✓)
        ↓
┌─────────────────────────────────────┐
│ FASE 3: Inicialização Automática    │
└─────────────────────────────────────┘
        ↓
  ⚡ vault-init (quando Vault ✓)
     └─→ Cria secrets em secret/dev/*
     └─→ Exit code 0 → COMPLETO
        ↓
  ⚡ kong-init (quando Kong + Keycloak ✓)
     └─→ Registra OIDC em Kong
     └─→ Exit code 0 → COMPLETO
        ↓
🎉 INFRAESTRUTURA PRONTA
```

---

## ⏱️ Timing Esperado

| Fase | Container | Tempo de Startup | Status |
|------|-----------|-----------------|--------|
| 1 | PostgreSQL | 5-10s | healthy |
| 1 | Redis | 3-5s | healthy |
| 1 | RabbitMQ | 5-10s | healthy |
| 1 | Prometheus | 10-15s | healthy |
| 1 | Vault | 15-20s | healthy |
| 2 | Keycloak | 20-30s | healthy |
| 2 | Kong | 15-25s | healthy |
| 2 | Grafana | 10-20s | healthy |
| 3 | vault-init | 5-10s | Exited (0) ← REMOVER |
| 3 | kong-init | 5-10s | Exited (0) ← REMOVER |

**Tempo Total:** ~90-120 segundos até pronto para uso

---

## 📍 Monitoramento de Status

### Verificar status em tempo real:
```powershell
# Ver todos os containers
docker-compose ps

# Ver apenas containers em execução
docker-compose ps --filter "status=running"

# Monitorar logs de um serviço
docker-compose logs -f vault-init      # Vault initialization
docker-compose logs -f kong-init       # Kong OIDC setup
docker-compose logs -f keycloak        # Keycloak startup
```

### Sinais de Sucesso:

✅ **Vault Init Completo:**
```
Vault está acessível!
   Status: UNSEALED
✓  Secret criado com sucesso (postgres)
✓  Secret criado com sucesso (redis)
✓  Secret criado com sucesso (rabbitmq)
✓  Secret criado com sucesso (grafana)
✓  Secret criado com sucesso (vault)
Vault initialized successfully. Container can now be removed.
```

✅ **Kong Init Completo:**
```
Creating Service: agile-api
Creating Route: agile-api-route
Registering OIDC Plugin
Kong OIDC configured successfully. Container can now be removed.
```

---

## 🧹 Limpeza de Containers de Inicialização

### ⚙️ Método 1: Script Automático (RECOMENDADO)

```powershell
# Navegar até o repositório
cd F:\Projetos\AgileWorkers\arquitetura-backend-2026

# Executar script de limpeza
.\scripts\Cleanup-Init-Containers.ps1
```

**O script irá:**
✓ Verificar logs de ambos os containers  
✓ Remover agile-vault-init  
✓ Remover agile-kong-init  
✓ Exibir resumo da limpeza  

### ⚙️ Método 2: Comandos Manuais

```powershell
# Remover vault-init
docker rm -f agile-vault-init

# Remover kong-init
docker rm -f agile-kong-init

# Verificar que foram removidos
docker-compose ps
```

### ⚙️ Método 3: Remover Via Docker Compose

```powershell
docker-compose rm -f agile-vault-init agile-kong-init
```

---

## ⚠️ Considerações Importantes

### 🔴 NÃO REMOVA:
- ❌ `agile-postgres` - Banco de dados principal
- ❌ `agile-redis` - Cache distribuído
- ❌ `agile-rabbitmq` - Message broker
- ❌ `agile-vault` - Gerenciador de secrets
- ❌ `agile-kong` - API Gateway
- ❌ `agile-keycloak` - Identity Provider
- ❌ `agile-prometheus` / `agile-grafana` - Observabilidade
- ❌ `agile-jaeger` - Tracing distribuído

### ✅ SEGURO REMOVER:
- ✅ `agile-vault-init` - Apenas executa uma vez
- ✅ `agile-kong-init` - Apenas configura na startup

---

## 🔍 Troubleshooting

### Problema: vault-init ainda em execução após 30s

```powershell
# Ver logs detalhados
docker-compose logs vault-init

# Possíveis causas:
# 1. Vault ainda inicializando → aguarde mais 10-20s
# 2. Conexão com Vault falhou → verificar logs do vault
# 3. Erro no script → revisar curl requests
```

### Problema: kong-init falhou

```powershell
# Ver logs
docker-compose logs kong-init

# Possíveis causas:
# 1. Keycloak não está saudável → verificar logs do keycloak
# 2. Kong ainda inicializando → aguarde mais 20-30s
# 3. Client OAuth não registrado → configurar manualmente no Keycloak

# Configuração manual:
# 1. Acessar http://localhost:8081/admin
# 2. Login: admin / agile_keycloak_password_123
# 3. Criar realm "agile"
# 4. Criar Client "kong-client" com secret
```

### Problema: "Container does not exist" ao tentar remover

```powershell
# É normal - significa que já foi removido anteriormente
# Verifique com:
docker ps -a | grep init

# Se não aparecer, container já foi removido com sucesso ✓
```

---

## 📝 Status Pós-Inicialização

Após remover os containers de inicialização, seu `docker-compose ps` deve mostrar:

```
NAME                IMAGE                         STATUS
agile-postgres      postgres:16-alpine            Up (healthy)
agile-redis         redis:7-alpine                Up (healthy)
agile-rabbitmq      rabbitmq:3.12-management     Up (healthy)
agile-vault         hashicorp/vault:1.15          Up (healthy)
agile-keycloak      keycloak/keycloak:latest      Up (healthy)
agile-kong          kong:3.4                      Up (healthy)
agile-prometheus    prom/prometheus:latest        Up (healthy)
agile-grafana       grafana/grafana:latest        Up (healthy)
agile-jaeger        jaegertracing/all-in-one      Up (unhealthy) ⚠️
agile-vault-init    REMOVIDO ✓
agile-kong-init     REMOVIDO ✓
```

---

## 🎯 Próximas Etapas

Após inicialização completa:

1. ✅ Verificar todos containers saudáveis: `docker-compose ps`
2. ✅ Remover containers de inicialização: `.\scripts\Cleanup-Init-Containers.ps1`
3. ✅ Acessar painéis:
   - Grafana: http://localhost:3000 (admin/agile_grafana_password_123)
   - Keycloak: http://localhost:8081/admin (admin/agile_keycloak_password_123)
   - Kong Manager: http://localhost:8002
   - Jaeger: http://localhost:16686
   - Prometheus: http://localhost:9090
   - Vault: http://localhost:8200/ui
4. ✅ Iniciar backend .NET 10+ com configuração OpenTelemetry
5. ✅ Executar testes de integração

---

## 📚 Referências

- [Docker Compose - restart policy](https://docs.docker.com/compose/compose-file/#restart_policy)
- [Vault - Dev Server](https://www.vaultproject.io/docs/concepts/dev-server)
- [Kong - Admin API](https://docs.konghq.com/gateway/latest/admin-api/)
- [Keycloak - Server Administration](https://www.keycloak.org/docs/latest/server_admin/)
