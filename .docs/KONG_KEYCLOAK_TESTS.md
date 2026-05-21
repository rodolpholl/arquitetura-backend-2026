# 🧪 Kong + Keycloak Integration Tests

**Teste prático:** Verificar que Kong está protegendo APIs com tokens Keycloak OIDC.

---

## 📋 Prerequisites

- Docker stack rodando: `docker-compose up -d`
- Kong pronto: `http://localhost:8001/status` (HTTP 200)
- Keycloak pronto: `http://localhost:8080/health/ready` (HTTP 200)
- Realm "agile" e cliente "kong-client" configurados (ver KEYCLOAK_SETUP_GUIDE.md)

---

## 🧪 Teste 1: Verificar Kong Service & Route

### Objetivo
Confirmar que Kong tem o service e route configurados.

### Comando

**PowerShell:**
```powershell
# Listar services
Invoke-WebRequest -Uri "http://localhost:8001/services" `
  -Method GET | Select-Object -ExpandProperty Content | ConvertFrom-Json | 
  Select-Object -ExpandProperty data | 
  Format-Table name, url

# Listar routes
Invoke-WebRequest -Uri "http://localhost:8001/routes" `
  -Method GET | Select-Object -ExpandProperty Content | ConvertFrom-Json |
  Select-Object -ExpandProperty data |
  Format-Table name, protocols, hosts, paths
```

**Bash/cURL:**
```bash
# Listar services
curl -s http://localhost:8001/services | jq '.data[] | {name, url, protocol}'

# Listar routes
curl -s http://localhost:8001/routes | jq '.data[] | {name, protocols, hosts, paths}'
```

### Expected Output
```json
{
  "name": "agile-api",
  "url": "http://agile-api:5000",
  "protocol": "http"
}

{
  "name": "agile-api-route",
  "protocols": ["http"],
  "hosts": ["api.localhost"],
  "paths": ["/api"]
}
```

✅ **PASS** → Service e route existem  
❌ **FAIL** → Verificar kong-init log

---

## 🧪 Teste 2: Verificar Plugin OIDC

### Objetivo
Confirmar que o plugin OIDC está ativo na route.

### Comando

**PowerShell:**
```powershell
Invoke-WebRequest -Uri "http://localhost:8001/routes/agile-api-route/plugins" `
  -Method GET | Select-Object -ExpandProperty Content | ConvertFrom-Json |
  Select-Object -ExpandProperty data |
  Format-Table name, enabled
```

**Bash/cURL:**
```bash
curl -s http://localhost:8001/routes/agile-api-route/plugins | \
  jq '.data[] | {name, enabled, config}'
```

### Expected Output
```json
{
  "name": "oidc",
  "enabled": true,
  "config": {
    "client_id": "kong-client",
    "discovery": "http://keycloak:8080/realms/agile/.well-known/openid-configuration",
    ...
  }
}
```

✅ **PASS** → Plugin OIDC ativo  
❌ **FAIL** → Kong init não completou

---

## 🧪 Teste 3: Obter Token do Keycloak

### Objetivo
Autenticar com Keycloak e obter JWT token.

### Comando

**PowerShell:**
```powershell
$params = @{
    Uri = "http://localhost:8080/realms/agile/protocol/openid-connect/token"
    Method = "POST"
    Headers = @{"Content-Type" = "application/x-www-form-urlencoded"}
    Body = "client_id=kong-client&client_secret=kong-secret&grant_type=client_credentials"
}

$response = Invoke-WebRequest @params
$token = ($response.Content | ConvertFrom-Json).access_token
Write-Host "Token obtido: $($token.Substring(0, 50))..."
```

**Bash/cURL:**
```bash
TOKEN=$(curl -s -X POST \
  http://localhost:8080/realms/agile/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=kong-client" \
  -d "client_secret=kong-secret" \
  -d "grant_type=client_credentials" | jq -r '.access_token')

echo "Token: $TOKEN"
```

### Expected Output
```
Token obtido: eyJhbGciOiJSUzI1NiIsInR5cCI...
```

✅ **PASS** → Token obtido com sucesso  
❌ **FAIL** → Verificar Keycloak config

---

## 🧪 Teste 4: Testar Acesso SEM Token (Deve ser Bloqueado)

### Objetivo
Confirmar que Kong bloqueia requisições sem autenticação.

### Comando

**PowerShell:**
```powershell
try {
    Invoke-WebRequest -Uri "http://localhost:8000/api/" `
        -Method GET -ErrorAction Stop
} catch {
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
    Write-Host "Body: $($_.Exception.Response | Get-Content)"
}
```

**Bash/cURL:**
```bash
curl -v http://localhost:8000/api/
```

### Expected Output
```
HTTP/1.1 302 Found
Location: http://localhost:8080/realms/agile/protocol/openid-connect/auth?...
```

✅ **PASS** → Redirecionado para login Keycloak (302)  
❌ **FAIL** → Plugin OIDC não ativo?

---

## 🧪 Teste 5: Testar Acesso COM Token Válido

### Objetivo
Confirmar que Kong aceita requisições com token válido.

### Comando

**PowerShell:**
```powershell
# 1. Obter token
$tokenResponse = Invoke-WebRequest -Uri "http://localhost:8080/realms/agile/protocol/openid-connect/token" `
    -Method POST `
    -Headers @{"Content-Type" = "application/x-www-form-urlencoded"} `
    -Body "client_id=kong-client&client_secret=kong-secret&grant_type=client_credentials"

$token = ($tokenResponse.Content | ConvertFrom-Json).access_token

# 2. Usar token
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8000/api/" `
        -Method GET `
        -Headers @{"Authorization" = "Bearer $token"}
    
    Write-Host "Status: $($response.StatusCode)"
    Write-Host "Headers: $($response.Headers | ConvertTo-Json)"
} catch {
    Write-Host "Erro: $($_.Exception.Message)"
}
```

**Bash/cURL:**
```bash
# 1. Obter token
TOKEN=$(curl -s -X POST \
  http://localhost:8080/realms/agile/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=kong-client&client_secret=kong-secret&grant_type=client_credentials" | \
  jq -r '.access_token')

# 2. Usar token
curl -v \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:8000/api/
```

### Expected Output
```
HTTP/1.1 200 OK
(ou HTTP/1.1 404 Not Found se o serviço real não existir)
```

✅ **PASS** → Requisição passou pelo Kong (200/404)  
❌ **FAIL** → Token inválido ou plugin com problema

---

## 🧪 Teste 6: Testar com Token Inválido

### Objetivo
Confirmar que Kong rejeita tokens inválidos.

### Comando

**PowerShell:**
```powershell
$badToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.invalid.invalid"

try {
    Invoke-WebRequest -Uri "http://localhost:8000/api/" `
        -Method GET `
        -Headers @{"Authorization" = "Bearer $badToken"} `
        -ErrorAction Stop
} catch {
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
}
```

**Bash/cURL:**
```bash
curl -v \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.invalid.invalid" \
  http://localhost:8000/api/
```

### Expected Output
```
HTTP/1.1 401 Unauthorized
{"error":"Unauthorized"}
```

✅ **PASS** → Token rejeitado (401)  
❌ **FAIL** → Plugin não validando tokens?

---

## 🧪 Teste 7: Verificar Kong Manager UI

### Objetivo
Validar que Kong Manager consegue listar configurações.

### Acesso
```
http://localhost:8002
```

### Verificações
- [ ] Menu "Services" mostra "agile-api"
- [ ] Menu "Routes" mostra "agile-api-route"
- [ ] Route tem plugin OIDC ativo
- [ ] Kong Dashboard mostra status OK

✅ **PASS** → UI funcional e configuração visível  
❌ **FAIL** → Problema com Kong Manager

---

## 🧪 Teste 8: Validar OIDC Discovery

### Objetivo
Confirmar que Keycloak expõe metadata OIDC corretamente.

### Comando

**PowerShell:**
```powershell
$discovery = Invoke-WebRequest -Uri "http://localhost:8080/realms/agile/.well-known/openid-configuration" `
    -Method GET | Select-Object -ExpandProperty Content | ConvertFrom-Json

Write-Host "Issuer: $($discovery.issuer)"
Write-Host "Token Endpoint: $($discovery.token_endpoint)"
Write-Host "Authorization Endpoint: $($discovery.authorization_endpoint)"
Write-Host "JWKS URI: $($discovery.jwks_uri)"
```

**Bash/cURL:**
```bash
curl -s http://localhost:8080/realms/agile/.well-known/openid-configuration | \
  jq '{issuer, token_endpoint, authorization_endpoint, jwks_uri}'
```

### Expected Output
```json
{
  "issuer": "http://localhost:8080/realms/agile",
  "token_endpoint": "http://localhost:8080/realms/agile/protocol/openid-connect/token",
  "authorization_endpoint": "http://localhost:8080/realms/agile/protocol/openid-connect/auth",
  "jwks_uri": "http://localhost:8080/realms/agile/protocol/openid-connect/certs"
}
```

✅ **PASS** → OIDC Discovery funcional  
❌ **FAIL** → Problema Keycloak

---

## 📊 Test Report Template

```markdown
# Kong + Keycloak Integration Test Report
Data: [DATA]
Executor: [NOME]

## Resultados

| Teste | Status | Detalhes |
|-------|--------|----------|
| 1. Kong Service/Route | ✅ PASS | Service e route existem |
| 2. Plugin OIDC | ✅ PASS | Plugin ativo e configurado |
| 3. Token Keycloak | ✅ PASS | Token obtido com sucesso |
| 4. Sem Token (Bloqueado) | ✅ PASS | 302 redirect para login |
| 5. Com Token (Permitido) | ✅ PASS | 200/404 (serviço não existe) |
| 6. Token Inválido | ✅ PASS | 401 Unauthorized |
| 7. Kong Manager UI | ✅ PASS | UI acessível e funcional |
| 8. OIDC Discovery | ✅ PASS | Metadata expostas corretamente |

## Conclusão
✅ **INTEGRAÇÃO FUNCIONANDO** - Todos os testes passaram!

## Próximos Passos
- [ ] Implementar autorização por roles
- [ ] Testar rate limiting com Kong
- [ ] Implementar logging centralizado
```

---

## 🆘 Troubleshooting Rápido

| Problema | Solução |
|----------|---------|
| `Connection refused` em Kong | Kong container não iniciou - `docker ps` |
| `401 Unauthorized` | Token expirado ou secret incorreto |
| `302 redirect` mas não autentica | Verificar realm e cliente no Keycloak |
| Kong Manager não carrega | Ligar `http://localhost:8002` sem redirect |
| Token válido mas ainda `401` | Limpar cache - `docker-compose restart kong` |
| CORS error | Configurar `Web Origins` em Keycloak |

---

**Status:** ✅ Testes prontos para execução  
**Versão:** Kong 3.4 + Keycloak latest  
**Data:** Maio 2026
