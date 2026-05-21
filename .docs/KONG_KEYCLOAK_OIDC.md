# 🔐 Kong + Keycloak OIDC Integration

**Data:** Maio 2026  
**Status:** Configuração Automática Iniciada

## 📋 Overview

Kong está configurado para usar **Keycloak** como provedor OAuth 2.0 / OpenID Connect (OIDC).  
Toda requisição para as APIs protegidas será validada contra tokens JWT do Keycloak.

## 🚀 Quick Start

### 1️⃣ Iniciar a Infraestrutura

```bash
docker-compose up -d
```

### 2️⃣ Acessar Keycloak Admin Console

**URL:** [http://localhost:8080](http://localhost:8080)

```
Username: admin
Password: agile_keycloak_password_123
```

### 3️⃣ Criar Realm "agile" (Recomendado)

1. Clique em dropdown "Master" (canto superior esquerdo)
2. Clique em "Create Realm"
3. Nome: `agile`
4. Clique "Create"

### 4️⃣ Criar Cliente Kong

Dentro do realm "agile":

1. Ir para **Clients** (menu esquerdo)
2. Clique **Create client**
3. Configure:
   - **Client ID:** `kong-client`
   - **Client Protocol:** `openid-connect`
   - Clique **Next**

4. Em **Capability config:**
   - ✅ Enable client authentication
   - ✅ Service accounts roles
   - Clique **Next**

5. Em **Login settings:**
   - **Valid redirect URIs:** 
     ```
     http://localhost:8000/api/
     http://localhost:8000/*
     ```
   - **Web Origins:** 
     ```
     http://localhost:8000
     ```
   - Clique **Save**

6. Ir para aba **Credentials**
   - Copie o **Client Secret**
   - Use: `kong-secret` (ou atualize no script kong-init.sh)

### 5️⃣ Configurar Kong (Automático)

O container `kong-init` executa automaticamente após inicialização:

```bash
✅ Kong pronto!
✅ Keycloak pronto!
📋 Configurando Kong com Keycloak OIDC...
  1️⃣ Criando Kong Service...
  2️⃣ Criando Kong Route...
  3️⃣ Configurando OIDC Plugin...
```

## 📌 Verificar Configuração

### Kong Manager UI
```
http://localhost:8002
```
- Verificar Services, Routes e Plugins
- Procurar por: `agile-api` service com `oidc` plugin

### Testar Fluxo de Autenticação

```bash
# 1. Tentar acessar rota protegida (deve redirecionar para login)
curl -v http://localhost:8000/api/

# 2. Login no Keycloak e obter token
curl -X POST http://localhost:8080/realms/agile/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=kong-client" \
  -d "client_secret=kong-secret" \
  -d "grant_type=client_credentials"

# 3. Usar token em requisição
curl -H "Authorization: Bearer <TOKEN>" http://localhost:8000/api/
```

## 🔧 Configuração Personalizada

### Alterar Client Secret

Se não usar `kong-secret`, atualize o script:

```bash
# docker-init/kong/kong-init.sh - linha com config.client_secret
--data config.client_secret=seu_secret_aqui
```

### Usar Realm "master" ao invés de "agile"

```bash
# docker-init/kong/kong-init.sh - linha com discovery
--data config.discovery="http://keycloak:8080/realms/master/.well-known/openid-configuration"
```

### Scopes Adicionais

```bash
# docker-init/kong/kong-init.sh - linha com scope
--data config.scope="openid profile email roles"
```

## 📚 Arquitetura

```
┌─────────────┐
│   Cliente   │
└──────┬──────┘
       │ Requisição com Auth Header
       ▼
┌─────────────────┐          ┌──────────────┐
│  Kong Gateway   │◄────────►│  Keycloak    │
│  (OIDC Plugin)  │ Valida   │  (OIDC IdP)  │
│  Port 8000      │ Tokens   │  Port 8080   │
└────────┬────────┘          └──────────────┘
         │ Forwards se válido
         ▼
    ┌─────────────┐
    │  API Real   │
    │  (Seu App)  │
    └─────────────┘
```

## 🛡️ Fluxo de Token

1. **Cliente** faz requisição → Kong
2. **Kong** vê OIDC plugin ativo
3. **Kong** redireciona para login Keycloak (se sem token)
4. **Keycloak** autentica usuário
5. **Keycloak** emite JWT token
6. **Cliente** envia token em header `Authorization: Bearer <JWT>`
7. **Kong** valida JWT contra Keycloak
8. **Kong** forwards requisição para API real

## ⚙️ Variáveis de Ambiente Kong

| Variável | Valor |
|----------|-------|
| `KONG_PLUGINS` | `bundled,oidc` |
| `KONG_DATABASE` | `postgres` |
| `KONG_ADMIN_LISTEN` | `0.0.0.0:8001` |
| `KONG_ADMIN_GUI_URL` | `http://localhost:8002` |

## 🔗 Referências

- [Kong OIDC Plugin](https://github.com/Nokia/kong-oidc)
- [Keycloak OIDC Protocol](https://www.keycloak.org/docs/latest/securing_apps/#openid-connect)
- [Kong Admin API](https://docs.konghq.com/gateway/latest/admin-api/)

## 📝 Próximos Passos

1. ✅ Infraestrutura iniciada
2. ✅ Kong + Keycloak integrados
3. ⏳ Criar primeiros usuários no Keycloak
4. ⏳ Proteger endpoints da API com autorizações por role
5. ⏳ Configurar rate limiting por tenant

---

**Status:** Configuração de OIDC concluída | Kong pronto para autenticação centralizada 🚀
