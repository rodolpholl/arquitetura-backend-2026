# Docker Compose - Agile Workers Backend

Infraestrutura completa para desenvolvimento local com PostgreSQL, Redis, RabbitMQ, Vault, Jaeger, Prometheus e Grafana.

## 📦 Serviços

| Serviço | Porta | Credenciais |
|---------|-------|------------|
| **PostgreSQL** | 5432 | agile_admin / agile_dev_password_123 |
| **Redis** | 6379 | password: agile_redis_password_123 |
| **RabbitMQ** | 5672 / 15672 | agile_user / agile_rabbitmq_password_123 |
| **Vault** | 8200 | Token: agile_dev_token_12345 |
| **Jaeger** | 16686 | - |
| **Prometheus** | 9090 | - |
| **Grafana** | 3000 | admin / agile_grafana_password_123 |

## 🚀 Quick Start (Execute em ordem)

### 1️⃣ Iniciar Todos os Serviços
```bash
docker-compose up -d
```

Aguarde 30 segundos para os serviços estabilizarem.

### 2️⃣ Verificar Status
```bash
docker-compose ps
```

Todos devem estar com status "healthy" ou "running".

### 3️⃣ Inicializar Secrets no Vault
```bash
# Opção 1: Via Makefile (recomendado)
make vault-init

# Opção 2: Via PowerShell
.\scripts\Initialize-Vault.ps1 -Environment dev

# Opção 3: Via Vault CLI manualmente
export VAULT_ADDR='http://localhost:8200'
export VAULT_TOKEN='agile_dev_token_12345'

vault kv put secret/dev/postgres username="agile_admin" password="agile_dev_password_123"
vault kv put secret/dev/redis password="agile_redis_password_123"
vault kv put secret/dev/rabbitmq username="agile_user" password="agile_rabbitmq_password_123"
vault kv put secret/dev/grafana admin_password="agile_grafana_password_123"
```

### 4️⃣ Acessar as UIs
- 🔐 **Vault:** http://localhost:8200/ui (Token: agile_dev_token_12345)
- 📊 **RabbitMQ:** http://localhost:15672 (agile_user / agile_rabbitmq_password_123)
- 📈 **Prometheus:** http://localhost:9090
- 📉 **Jaeger:** http://localhost:16686
- 🎨 **Grafana:** http://localhost:3000 (admin / agile_grafana_password_123)

## 🛠️ Comandos Úteis

```bash
# Parar todos os serviços
docker-compose down

# Remover volumes e dados (CUIDADO!)
docker-compose down -v

# Ver logs de um serviço
docker-compose logs -f postgres
docker-compose logs -f rabbitmq
docker-compose logs -f vault

# Ver status
docker-compose ps

# Vault CLI
make vault-list              # Listar secrets
make vault-read SECRET=postgres  # Ler um secret
make vault-ui                # Abrir UI
```

## 🔌 Connection Strings

```
PostgreSQL: Server=localhost;Port=5432;Database=agile_lancamentos;User Id=agile_admin;Password=agile_dev_password_123;
Redis: localhost:6379,password=agile_redis_password_123
RabbitMQ: amqp://agile_user:agile_rabbitmq_password_123@localhost:5672/agile
Vault: http://localhost:8200
Jaeger OTLP: http://localhost:14268/api/traces
```

## 🔐 Segurança

- ✅ **Secrets:** Armazenados em Vault (nunca em Git)
- ✅ **Arquivos:** `.env.docker` ignorado pelo Git (arquivo local)
- ✅ **Template:** `.env.docker.example` está versionado (seguro)

## 📁 Arquivos de Inicialização

```
docker-init/
├── postgres/init-databases.sql
├── rabbitmq/rabbitmq.conf
├── rabbitmq/definitions.json
├── prometheus/prometheus.yml
└── grafana/provisioning/
```

## ⚠️ Troubleshooting

**Serviço não inicia:**
```bash
docker-compose logs -f <nome_servico>
docker-compose rm -f <nome_servico>
docker-compose up <nome_servico>
```

**Porta em uso:**
```bash
# Linux/Mac: lsof -i :5432
# Windows: netstat -ano | findstr :5432
```

---

**Versão:** 1.0  
**Última atualização:** Maio 2026  
**Status:** ✅ Production-Ready (Com adaptações para ambiente local)
