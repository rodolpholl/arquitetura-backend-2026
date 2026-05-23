# Kong + Keycloak OIDC Integration

**Status:** Configuracao automatica via `kong-init`

## Overview

Kong e configurado como API Gateway com duas camadas de autenticacao:

1. **Key-Auth (X-Subscription-Key)** — primeira camada, verifica se o consumer tem chave valida
2. **OIDC (JWT do Keycloak)** — segunda camada, valida o token Bearer no header `Authorization`

A configuracao e feita automaticamente pelo container `kong-init` apos `keycloak-init` completar.

---

## Arquitetura

```
Cliente
    |
    | POST /lancamentos  +  X-Subscription-Key  +  Authorization: Bearer <JWT>
    v
Kong Gateway (:8000)
    |-- 1. key-auth plugin: valida X-Subscription-Key contra secret/dev/kong
    |-- 2. oidc plugin:     valida JWT contra Keycloak (:8081/realms/fincontrol)
    |
    v (se ambos OK)
FinControl.Lancamentos.API (:5000)  ou  FinControl.Consolidado.API (:5001)
    |
    v
SubscriptionKeyMiddleware (segunda camada interna — cobre requests que bypassam o Kong)
```

---

## Configuracao do Kong

### Services e Routes

| Service | URL interna | Route path |
|---------|-------------|-----------|
| `fincontrol-lancamentos` | `http://host.docker.internal:5000` | `/lancamentos` |
| `fincontrol-consolidados` | `http://host.docker.internal:5001` | `/consolidados` |

### Plugins Ativos

| Plugin | Escopo | Config |
|--------|--------|--------|
| `key-auth` | Global | Header: `X-Subscription-Key` |
| `oidc` | Global | Discovery: `http://keycloak:8080/realms/fincontrol/.well-known/openid-configuration` |

### Consumers e Credentials

| Consumer | Subscription Key (dev) |
|----------|----------------------|
| `lancamentos-consumer` | `fc-lanc-dev-subkey-2026-abc123ef` |
| `consolidados-consumer` | `fc-cons-dev-subkey-2026-xyz789ab` |

As subscription keys vem do Vault (`secret/dev/kong`) e sao provisionadas pelo `kong-init.sh`.

---

## Quick Start

### 1. Iniciar infraestrutura

```bash
docker-compose up -d
```

Aguardar todos os init containers terminarem com `Exited (0)`.

### 2. Verificar que Kong esta configurado

```bash
# Listar services
curl -s http://localhost:8001/services | jq '.data[].name'

# Listar plugins ativos
curl -s http://localhost:8001/plugins | jq '.data[] | {name, enabled}'
```

### 3. Obter token do Keycloak

```bash
TOKEN=$(curl -s -X POST \
  http://localhost:8081/realms/fincontrol/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=fincontrol-api&client_secret=fincontrol-api-secret&grant_type=password&username=dev.user&password=Dev@123456!" \
  | jq -r '.access_token')
```

### 4. Fazer requisicao autenticada

```bash
curl -s http://localhost:8000/lancamentos/health \
  -H "X-Subscription-Key: fc-lanc-dev-subkey-2026-abc123ef" \
  -H "Authorization: Bearer $TOKEN"
```

---

## Configuracao Manual (se kong-init falhar)

### Criar Service

```bash
curl -X POST http://localhost:8001/services \
  -d name=fincontrol-lancamentos \
  -d url=http://host.docker.internal:5000
```

### Criar Route

```bash
curl -X POST http://localhost:8001/services/fincontrol-lancamentos/routes \
  -d name=lancamentos-route \
  -d "paths[]=/lancamentos"
```

### Habilitar key-auth

```bash
curl -X POST http://localhost:8001/plugins \
  -d name=key-auth \
  -d "config.key_names[]=X-Subscription-Key"
```

### Criar Consumer e Credential

```bash
curl -X POST http://localhost:8001/consumers \
  -d username=lancamentos-consumer

curl -X POST http://localhost:8001/consumers/lancamentos-consumer/key-auth \
  -d key=fc-lanc-dev-subkey-2026-abc123ef
```

### Habilitar OIDC

```bash
curl -X POST http://localhost:8001/plugins \
  -d name=oidc \
  -d "config.client_id=kong-client" \
  -d "config.client_secret=kong-secret" \
  -d "config.discovery=http://keycloak:8080/realms/fincontrol/.well-known/openid-configuration"
```

---

## Variaveis de Ambiente Kong

| Variavel | Valor |
|----------|-------|
| `KONG_DATABASE` | `postgres` |
| `KONG_PG_HOST` | `postgres` |
| `KONG_PG_DATABASE` | `kong` |
| `KONG_ADMIN_LISTEN` | `0.0.0.0:8001` |
| `KONG_ADMIN_GUI_URL` | `http://localhost:8002` |
| `KONG_PLUGINS` | `bundled` |

---

## Troubleshooting

| Problema | Solucao |
|----------|---------|
| `No API key found` | Adicionar header `X-Subscription-Key` |
| `Invalid authentication credentials` | Verificar chave no Vault `secret/dev/kong` |
| `Unauthorized` (401) | Token JWT expirado ou issuer incorreto |
| OIDC discovery falha | Verificar se Keycloak esta rodando em `:8081` |
| kong-init falhou | `docker-compose logs kong-init` |

---

## Referencias

- [Kong Admin API](https://docs.konghq.com/gateway/latest/admin-api/)
- [Keycloak OIDC](https://www.keycloak.org/docs/latest/securing_apps/#openid-connect)
- [KEYCLOAK_SETUP_GUIDE.md](KEYCLOAK_SETUP_GUIDE.md)
- [KONG_KEYCLOAK_TESTS.md](KONG_KEYCLOAK_TESTS.md)

---

**Versao:** 2.0
**Ultima atualizacao:** Maio 2026
**Status:** Ativo — configuracao automatizada via kong-init
