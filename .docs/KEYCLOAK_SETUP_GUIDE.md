# Keycloak Setup - FinControl

## Objetivo

Configurar Keycloak com realm **fincontrol** e clientes **kong-client** e **fincontrol-api** para integracao com Kong Gateway e as APIs .NET.

> **Nota:** A configuracao abaixo e feita **automaticamente** pelo container `keycloak-init` ao executar `docker-compose up -d`. Este guia serve para configuracao manual ou para recriar o ambiente em caso de falha do init.

---

## Acesso ao Keycloak Admin Console

```
URL:      http://localhost:8081
Username: admin
Password: fincontrol_keycloak_password_123
```

---

## Passo 1: Criar Realm "fincontrol"

### 1.1 - Menu de Realms
```
[Canto superior esquerdo]
  v "Master" (dropdown)
  |- Create Realm
```

### 1.2 - Dados do Novo Realm
```
Realm name:   fincontrol
Display name: FinControl Backend
```

Clique **Create**.

---

## Passo 2: Criar Cliente Kong (key-auth + OIDC)

### 2.1 - Ir para Clients

```
Menu esquerdo: Clients > Create client
```

### 2.2 - General Settings

| Campo | Valor |
|-------|-------|
| **Client ID** | `kong-client` |
| **Client Protocol** | `openid-connect` |

### 2.3 - Capability Config

```
[x] Client authentication
[x] Service accounts roles
[ ] Standard flow (desabilitar para M2M)
[x] Direct access grants
```

### 2.4 - Login Settings

```
Valid redirect URIs:
  http://localhost:8000/*

Web Origins:
  http://localhost:8000
```

Clique **Save**.

### 2.5 - Obter Client Secret

```
Aba: Credentials
Credential type: Client secret
[Copie o valor gerado]
```

O `keycloak-init` salva esse secret no Vault automaticamente em `secret/dev/keycloak → kong_client_secret`.

---

## Passo 3: Criar Cliente fincontrol-api (Swagger / M2M)

Repita o processo acima com:

| Campo | Valor |
|-------|-------|
| **Client ID** | `fincontrol-api` |
| Client authentication | habilitado |
| Direct access grants | habilitado |

---

## Passo 4: Criar Roles

```
Menu esquerdo: Realm roles > Create role
```

| Role | Descricao |
|------|-----------|
| `api-user` | Acesso de leitura/escrita nas APIs |
| `admin` | Acesso administrativo |

---

## Passo 5: Criar Usuarios de Teste

### 5.1 - Criar usuario

```
Menu esquerdo: Users > Add user

Username:       dev.user
Email:          dev.user@fincontrol.local
Email verified: [x]
First name:     Dev
Last name:      User
Enabled:        [x]
```

### 5.2 - Definir senha

```
Aba: Credentials > Set password
Password:   Dev@123456!
Temporary:  [ ] (desmarcar)
```

### 5.3 - Atribuir roles

```
Aba: Role mapping > Assign role
Selecionar: api-user
```

Repetir para `dev.admin` com password `Admin@123456!` e roles `api-user` + `admin`.

---

## Passo 6: Validar Configuracao

### OIDC Discovery

```bash
curl -s http://localhost:8081/realms/fincontrol/.well-known/openid-configuration | jq '{issuer, token_endpoint, jwks_uri}'
```

Resultado esperado:
```json
{
  "issuer": "http://localhost:8081/realms/fincontrol",
  "token_endpoint": "http://localhost:8081/realms/fincontrol/protocol/openid-connect/token",
  "jwks_uri": "http://localhost:8081/realms/fincontrol/protocol/openid-connect/certs"
}
```

### Obter Token (client_credentials)

```bash
curl -s -X POST http://localhost:8081/realms/fincontrol/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=kong-client&client_secret=kong-secret&grant_type=client_credentials" | jq .access_token
```

### Obter Token (password flow — usuario de teste)

```bash
curl -s -X POST http://localhost:8081/realms/fincontrol/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=fincontrol-api&client_secret=fincontrol-api-secret&grant_type=password&username=dev.user&password=Dev@123456!" | jq .access_token
```

---

## Checklist de Configuracao

- [ ] Realm "fincontrol" criado
- [ ] Cliente "kong-client" criado com client authentication habilitado
- [ ] Cliente "fincontrol-api" criado
- [ ] Client secrets salvos no Vault (`secret/dev/keycloak`)
- [ ] Roles `api-user` e `admin` criadas
- [ ] Usuarios `dev.user` e `dev.admin` criados com senhas permanentes
- [ ] OIDC discovery retornando issuer correto
- [ ] Token obtido com sucesso

---

## Troubleshooting

| Problema | Solucao |
|----------|---------|
| `Invalid client credentials` | Verificar client_secret no Vault: `secret/dev/keycloak → kong_client_secret` |
| Realm nao encontrado | Verificar se e `fincontrol` (nao `master` nem `agile`) |
| `connection refused` em :8081 | Keycloak ainda inicializando — aguardar healthcheck |
| Kong nao valida tokens | Issuer no Kong deve ser `http://localhost:8081/realms/fincontrol` |
| Container keycloak-init falhou | `docker-compose logs keycloak-init` para diagnostico |

---

**Versao:** 2.0
**Ultima atualizacao:** Maio 2026
**Status:** Ativo (configuracao automatizada via keycloak-init)
