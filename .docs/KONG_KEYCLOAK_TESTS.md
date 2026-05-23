# Kong + Keycloak Integration Tests

Testes praticos para verificar que Kong esta protegendo as APIs corretamente.

## Pre-requisitos

- `docker-compose up -d` executado e todos os inits com `Exited (0)`
- Kong respondendo: `http://localhost:8001/status` (HTTP 200)
- Keycloak respondendo: `http://localhost:8081/health/ready` (HTTP 200)
- Realm `fincontrol` e clientes configurados (ver [KEYCLOAK_SETUP_GUIDE.md](KEYCLOAK_SETUP_GUIDE.md))

---

## Teste 1: Verificar Services e Routes no Kong

### Objetivo
Confirmar que kong-init criou os services e routes esperados.

**PowerShell:**
```powershell
# Listar services
(Invoke-WebRequest -Uri "http://localhost:8001/services" -Method GET).Content |
  ConvertFrom-Json | Select-Object -ExpandProperty data |
  Format-Table name, url

# Listar routes
(Invoke-WebRequest -Uri "http://localhost:8001/routes" -Method GET).Content |
  ConvertFrom-Json | Select-Object -ExpandProperty data |
  Format-Table name, paths
```

**Bash:**
```bash
curl -s http://localhost:8001/services | jq '.data[] | {name, url}'
curl -s http://localhost:8001/routes | jq '.data[] | {name, paths}'
```

**Resultado esperado:**
```
fincontrol-lancamentos  →  /lancamentos
fincontrol-consolidados →  /consolidados
```

PASS → Services e routes existem
FAIL → Ver `docker-compose logs kong-init`

---

## Teste 2: Verificar Plugins Ativos

### Objetivo
Confirmar que key-auth e oidc estao habilitados.

**Bash:**
```bash
curl -s http://localhost:8001/plugins | jq '.data[] | {name, enabled}'
```

**Resultado esperado:**
```json
{"name": "key-auth", "enabled": true}
{"name": "oidc",     "enabled": true}
```

---

## Teste 3: Obter Token do Keycloak

### Objetivo
Autenticar com Keycloak e obter JWT.

**PowerShell:**
```powershell
$params = @{
    Uri     = "http://localhost:8081/realms/fincontrol/protocol/openid-connect/token"
    Method  = "POST"
    Headers = @{"Content-Type" = "application/x-www-form-urlencoded"}
    Body    = "client_id=fincontrol-api&client_secret=fincontrol-api-secret&grant_type=password&username=dev.user&password=Dev@123456!"
}
$TOKEN = (Invoke-WebRequest @params).Content | ConvertFrom-Json | Select-Object -ExpandProperty access_token
Write-Host "Token: $($TOKEN.Substring(0,50))..."
```

**Bash:**
```bash
TOKEN=$(curl -s -X POST \
  http://localhost:8081/realms/fincontrol/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=fincontrol-api&client_secret=fincontrol-api-secret&grant_type=password&username=dev.user&password=Dev@123456!" \
  | jq -r '.access_token')
echo "Token: ${TOKEN:0:50}..."
```

PASS → Token JWT retornado
FAIL → Verificar credenciais em [KEYCLOAK_SETUP_GUIDE.md](KEYCLOAK_SETUP_GUIDE.md)

---

## Teste 4: Acesso Sem Subscription Key (deve ser bloqueado)

### Objetivo
Confirmar que Kong rejeita requests sem `X-Subscription-Key`.

**Bash:**
```bash
curl -s -o /dev/null -w "%{http_code}" http://localhost:8000/lancamentos/health
# Esperado: 401
```

PASS → HTTP 401 com `{"message":"No API key found in request"}`
FAIL → Plugin key-auth nao esta ativo

---

## Teste 5: Acesso Sem Token JWT (deve ser bloqueado)

### Objetivo
Confirmar que Kong rejeita requests sem token JWT apos passar pelo key-auth.

**Bash:**
```bash
curl -s -o /dev/null -w "%{http_code}" \
  -H "X-Subscription-Key: fc-lanc-dev-subkey-2026-abc123ef" \
  http://localhost:8000/lancamentos/health
# Esperado: 401 ou 302 (redirect para login Keycloak)
```

---

## Teste 6: Acesso Com Ambas as Credenciais (deve passar)

### Objetivo
Confirmar que Kong encaminha request com key-auth + JWT validos.

**PowerShell:**
```powershell
# Obter token primeiro (Teste 3)
$response = Invoke-WebRequest -Uri "http://localhost:8000/lancamentos/health" `
    -Headers @{
        "X-Subscription-Key" = "fc-lanc-dev-subkey-2026-abc123ef"
        "Authorization"      = "Bearer $TOKEN"
    }
Write-Host "Status: $($response.StatusCode)"
```

**Bash:**
```bash
curl -s -o /dev/null -w "%{http_code}" \
  -H "X-Subscription-Key: fc-lanc-dev-subkey-2026-abc123ef" \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:8000/lancamentos/health
# Esperado: 200
```

PASS → HTTP 200 (ou 404 se API ainda nao subiu)
FAIL → Verificar token e subscription key

---

## Teste 7: Token JWT Invalido (deve ser rejeitado)

**Bash:**
```bash
curl -s -o /dev/null -w "%{http_code}" \
  -H "X-Subscription-Key: fc-lanc-dev-subkey-2026-abc123ef" \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiJ9.invalid.invalid" \
  http://localhost:8000/lancamentos/health
# Esperado: 401
```

---

## Teste 8: OIDC Discovery do Keycloak

**Bash:**
```bash
curl -s http://localhost:8081/realms/fincontrol/.well-known/openid-configuration | \
  jq '{issuer, token_endpoint, jwks_uri}'
```

**Resultado esperado:**
```json
{
  "issuer": "http://localhost:8081/realms/fincontrol",
  "token_endpoint": "http://localhost:8081/realms/fincontrol/protocol/openid-connect/token",
  "jwks_uri": "http://localhost:8081/realms/fincontrol/protocol/openid-connect/certs"
}
```

---

## Report Template

```
# Kong + Keycloak Integration Test Report
Data: [DATA]

| Teste | Status | Detalhes |
|-------|--------|----------|
| 1. Services/Routes     | PASS/FAIL | |
| 2. Plugins ativos      | PASS/FAIL | |
| 3. Token Keycloak      | PASS/FAIL | |
| 4. Sem subscription key| PASS/FAIL | HTTP 401 esperado |
| 5. Sem JWT             | PASS/FAIL | HTTP 401 esperado |
| 6. Ambas credenciais   | PASS/FAIL | HTTP 200 esperado |
| 7. JWT invalido        | PASS/FAIL | HTTP 401 esperado |
| 8. OIDC Discovery      | PASS/FAIL | |
```

---

## Troubleshooting

| Problema | Solucao |
|----------|---------|
| `Connection refused` em :8000 | Kong nao subiu — `docker-compose ps kong` |
| `No API key found` | Adicionar header `X-Subscription-Key` |
| `Invalid authentication credentials` | Verificar chave em Vault `secret/dev/kong` |
| `401` com token valido | Verificar issuer — deve ser `http://localhost:8081/realms/fincontrol` |
| Token expirado | Renovar token (TTL padrao: 300s) |
| CORS error | Configurar Web Origins em Keycloak |
| kong-init nao completou | `docker-compose logs kong-init` |

---

**Versao:** 2.0
**Ultima atualizacao:** Maio 2026
**Status:** Ativo
