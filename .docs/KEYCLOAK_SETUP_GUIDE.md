# Keycloak Setup - Passo a Passo

## 🎯 Objetivo

Configurar Keycloak com realm **agile** e cliente **kong-client** para integração com Kong Gateway.

---

## 📍 Passo 1: Acessar Keycloak Admin Console

```
URL: http://localhost:8080
Username: admin
Password: agile_keycloak_password_123
```

## 📍 Passo 2: Criar Realm "agile"

### 2.1 - Menu de Realms
```
[Canto superior esquerdo]
  ▼ "Master" (dropdown)
  ┗ Create Realm
```

### 2.2 - Dados do Novo Realm
```
Realm name: agile
Display name: Agile Workers Backend
```

### 2.3 - Clique "Create"

✅ Realm "agile" criado!

---

## 📍 Passo 3: Criar Cliente Kong

### 3.1 - Ir para Clients

```
Menu esquerdo:
  Realm settings
  ├─ Clients ◄─── CLIQUE AQUI
  ├─ Client scopes
  ├─ Roles
  └─ Users
```

### 3.2 - Criar Novo Cliente

```
Botão azul: "Create client"
```

### 3.3 - General Settings

| Campo | Valor |
|-------|-------|
| **Client ID** | `kong-client` |
| **Client Protocol** | `openid-connect` (já padrão) |
| **Client name** | Kong Gateway |

Clique "Next"

### 3.4 - Capability Config

```
✅ Client authentication (Enable)
✅ Authorization (Enable)
✅ Authentication flow binding overrides (Enable)
□ Standard flow disabled
✅ Direct access grants enabled
✅ Implicit flow enabled
✅ Service accounts roles
```

Clique "Next"

### 3.5 - Login Settings

```
Redirect URIs:
  ✅ http://localhost:8000/api/
  ✅ http://localhost:8000/*
  
Web Origins:
  ✅ http://localhost:8000
  ✅ +
  
Post Logout Redirect URIs:
  ✅ http://localhost:8000/api/
```

Clique "Save"

✅ Cliente "kong-client" criado!

---

## 📍 Passo 4: Obter Client Secret

### 4.1 - Ir para Aba "Credentials"

```
[Cliente kong-client já aberto]
  ├─ General (aba)
  ├─ Capability config (aba)
  ├─ Sessions (aba)
  ├─ Keys (aba)
  ├─ Roles (aba)
  ├─ Credentials ◄─── CLIQUE AQUI
  └─ Client scopes (aba)
```

### 4.2 - Copiar Client Secret

```
Credential type: Client secret
┌─────────────────────────────────────────────┐
│ XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    │
│ [Copy] ◄─── CLIQUE AQUI                    │
└─────────────────────────────────────────────┘
```

⚠️ **Guarde este secret com segurança!**

---

## 📍 Passo 5: Criar Usuário de Teste (Opcional)

### 5.1 - Ir para Users

```
Menu esquerdo:
  Manage
  ├─ Users ◄─── CLIQUE AQUI
  ├─ Groups
  ├─ Roles
  └─ Permissions
```

### 5.2 - Adicionar Usuário

```
Botão: "Add user"

Username: testuser
Email: test@agile.local
Email verified: ✅
First name: Test
Last name: User
Enabled: ✅

Clique "Create"
```

### 5.3 - Definir Senha

```
Aba: Credentials
┌──────────────┐
│ Set password │ ◄─── CLIQUE
└──────────────┘

Password: testuser123
Temporary: ❌ (desmarque para senha permanente)

Clique "Set password" ✅
```

---

## 📍 Passo 6: Criar Roles (Opcional mas Recomendado)

### 6.1 - Ir para Realm Roles

```
Menu esquerdo:
  Manage
  ├─ Roles ◄─── CLIQUE AQUI
```

### 6.2 - Criar Papel "api-user"

```
Botão: "Create role"

Role name: api-user
Description: Usuários com acesso a APIs
Display name: API User

Clique "Save"
```

### 6.3 - Criar Papel "admin"

```
Botão: "Create role"

Role name: admin
Description: Administradores do sistema
Display name: System Admin

Clique "Save"
```

---

## 📍 Passo 7: Atribuir Roles ao Usuário

### 7.1 - Ir para Users > testuser

```
Menu esquerdo: Users
Clique em "testuser"
Aba: "Role mapping"
```

### 7.2 - Atribuir Roles

```
"Assign role" dropdown:
  ┗ Select roles...
    ├─ api-user   ◄─── MARQUE
    └─ admin      ◄─── MARQUE (opcional)

Clique "Assign"
```

---

## 📍 Passo 8: Validar Configuração

### 8.1 - Verificar Cliente OIDC

```
URL da configuração OIDC:
  http://localhost:8080/realms/agile/.well-known/openid-configuration
```

Deve retornar:
```json
{
  "issuer": "http://localhost:8080/realms/agile",
  "authorization_endpoint": "http://localhost:8080/realms/agile/protocol/openid-connect/auth",
  "token_endpoint": "http://localhost:8080/realms/agile/protocol/openid-connect/token",
  ...
}
```

### 8.2 - Testar Obtenção de Token

```bash
curl -X POST http://localhost:8080/realms/agile/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=kong-client" \
  -d "client_secret=<SEGREDO_COPIADO>" \
  -d "grant_type=client_credentials"
```

Deve retornar:
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR...",
  "expires_in": 300,
  "refresh_expires_in": 1800,
  "token_type": "Bearer",
  ...
}
```

✅ Keycloak configurado com sucesso!

---

## 📚 Checklist de Configuração

- [ ] Realm "agile" criado
- [ ] Cliente "kong-client" criado  
- [ ] Client secret copiado
- [ ] Redirect URIs configuradas
- [ ] Web Origins configuradas
- [ ] Usuário "testuser" criado (opcional)
- [ ] Roles criadas (opcional)
- [ ] Token obtido com sucesso (teste)

---

## 🔗 Integrando com Kong

Agora Kong pode usar os tokens do Keycloak! O plugin OIDC do Kong vai:

1. Interceptar requisições em `http://localhost:8000/api/`
2. Validar tokens JWT contra Keycloak
3. Permitir requisições autenticadas
4. Bloquear ou redirecionar não autenticadas

**Kong Manager:** [http://localhost:8002](http://localhost:8002)

---

## 🆘 Troubleshooting

### Token inválido?
- ✅ Verificar se Client Secret está correto
- ✅ Verificar se realm é "agile" (não "master")
- ✅ Verificar `grant_type` na requisição

### CORS errors?
- ✅ Configurar Web Origins corretos
- ✅ Verificar se Kong está permitindo CORS

### Kong não encontra Keycloak?
- ✅ Verificar Docker network: `agile-network`
- ✅ Container kong consegue fazer ping para keycloak?
  ```bash
  docker exec agile-kong ping keycloak
  ```

---

**Versão:** Keycloak latest | Kong 3.4-alpine  
**Data:** Maio 2026  
**Status:** ✅ Configuração Completa
