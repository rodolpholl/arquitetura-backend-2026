#!/bin/bash
set -e

KONG_ADMIN="http://kong:8001"
KEYCLOAK_REALM_URL="http://keycloak:8080/realms/fincontrol"
VAULT_ADDR="${VAULT_ADDR:-http://vault:8200}"
VAULT_TOKEN="${VAULT_TOKEN:-fincontrol_dev_token_12345}"
VAULT_ENV="${VAULT_ENV:-dev}"

# ─── Helpers ────────────────────────────────────────────────────────────────

wait_for() {
  local url=$1 label=$2
  echo "⏳ Aguardando $label ficar pronto..."
  until curl -s -f -o /dev/null "$url"; do
    echo "  $label ainda não está pronto..."
    sleep 3
  done
  echo "✅ $label pronto!"
}

kong_upsert_service() {
  local name=$1 url=$2
  if curl -s -f -o /dev/null "$KONG_ADMIN/services/$name"; then
    echo "  ↩  Service '$name' já existe — atualizando URL..."
    curl -s -o /dev/null -X PATCH "$KONG_ADMIN/services/$name" \
      --data "url=$url"
  else
    echo "  ➕ Criando service '$name'..."
    curl -s -o /dev/null -X POST "$KONG_ADMIN/services" \
      --data "name=$name" \
      --data "url=$url" \
      --data "connect_timeout=10000" \
      --data "read_timeout=30000" \
      --data "write_timeout=30000" \
      --data "retries=3"
  fi
}

kong_upsert_route() {
  local service=$1 route_name=$2 path=$3 methods=$4 strip_path=${5:-false}
  if curl -s -f -o /dev/null "$KONG_ADMIN/routes/$route_name"; then
    echo "  ↩  Route '$route_name' já existe — ignorando..."
  else
    echo "  ➕ Criando route '$route_name' [$methods $path]..."
    local data="name=$route_name&paths[]=$path&strip_path=$strip_path&preserve_host=false"
    for m in $(echo "$methods" | tr ',' ' '); do
      data="$data&methods[]=$m"
    done
    curl -s -o /dev/null -X POST "$KONG_ADMIN/services/$service/routes" \
      --data "$data"
  fi
}

vault_get() {
  local path=$1 key=$2
  curl -s \
    -H "X-Vault-Token: $VAULT_TOKEN" \
    "$VAULT_ADDR/v1/secret/data/$VAULT_ENV/$path" \
    | grep -o "\"$key\":\"[^\"]*\"" | head -1 | sed 's/^"[^"]*":"\([^"]*\)"$/\1/'
}

kong_upsert_consumer() {
  local username=$1 subscription_key=$2

  if curl -s -f -o /dev/null "$KONG_ADMIN/consumers/$username"; then
    echo "  ↩  Consumer '$username' já existe — verificando credential..."
  else
    echo "  ➕ Criando consumer '$username'..."
    curl -s -o /dev/null -X POST "$KONG_ADMIN/consumers" \
      --data "username=$username" \
      --data "custom_id=$username"
  fi

  local existing_key
  existing_key=$(curl -s "$KONG_ADMIN/consumers/$username/key-auth" \
    | grep -o '"key":"[^"]*"' | head -1 | sed 's/"key":"\([^"]*\)"/\1/')

  if [ -n "$existing_key" ]; then
    echo "  ↩  Key-auth para '$username' já configurado — ignorando..."
  else
    echo "  ➕ Associando subscription key ao consumer '$username'..."
    curl -s -o /dev/null -X POST "$KONG_ADMIN/consumers/$username/key-auth" \
      --data "key=$subscription_key"
  fi
}

kong_upsert_plugin() {
  local target_type=$1  # "routes" ou "services"
  local target_name=$2
  local plugin_name=$3
  shift 3
  # Verifica se o plugin já está instalado no target
  local count
  count=$(curl -s "$KONG_ADMIN/$target_type/$target_name/plugins" \
    | grep -o "\"name\":\"$plugin_name\"" | wc -l || echo 0)
  if [ "$count" -gt 0 ]; then
    echo "  ↩  Plugin '$plugin_name' já instalado em $target_name — ignorando..."
  else
    echo "  ➕ Instalando plugin '$plugin_name' em $target_name..."
    curl -s -o /dev/null -X POST "$KONG_ADMIN/$target_type/$target_name/plugins" \
      --data "name=$plugin_name" \
      "$@"
  fi
}

# ─── Aguardar serviços ───────────────────────────────────────────────────────

wait_for "$KONG_ADMIN/status"  "Kong"
wait_for "$KEYCLOAK_REALM_URL" "Keycloak (realm fincontrol)"
wait_for "$VAULT_ADDR/v1/sys/health" "Vault"
sleep 3

# ─── Ler subscription keys do Vault ─────────────────────────────────────────
echo "🔑 Lendo subscription keys do Vault..."
LANC_SUBSCRIPTION_KEY=$(vault_get "kong" "lancamentos_subscription_key")
CONS_SUBSCRIPTION_KEY=$(vault_get "kong" "consolidados_subscription_key")

if [ -z "$LANC_SUBSCRIPTION_KEY" ]; then
  echo "  ⚠  lancamentos_subscription_key não encontrada no Vault — usando fallback dev"
  LANC_SUBSCRIPTION_KEY="fc-lanc-dev-subkey-2026-abc123ef"
fi

if [ -z "$CONS_SUBSCRIPTION_KEY" ]; then
  echo "  ⚠  consolidados_subscription_key não encontrada no Vault — usando fallback dev"
  CONS_SUBSCRIPTION_KEY="fc-cons-dev-subkey-2026-xyz789ab"
fi

echo "  ✅ Subscription keys carregadas do Vault"

echo ""
echo "═══════════════════════════════════════════════════════════"
echo "  Configurando Kong — FinControl API Gateway"
echo "═══════════════════════════════════════════════════════════"
echo ""

# ─── 1. SERVICE: fincontrol-lancamentos ─────────────────────────────────────
# POST /lancamentos/registrar
# Porta: 5083 (launchSettings.json / Lancamentos.API)

echo "▶ [1/2] Service: fincontrol-lancamentos (porta 5083)"
kong_upsert_service \
  "fincontrol-lancamentos" \
  "http://host.docker.internal:5083"

echo ""

# Route principal — cobre POST /lancamentos/registrar e futuras rotas sob /lancamentos
kong_upsert_route \
  "fincontrol-lancamentos" \
  "lancamentos-registrar" \
  "/lancamentos" \
  "POST,GET,OPTIONS"

echo ""

# Plugin: JWT Bearer (valida tokens Keycloak)
kong_upsert_plugin "services" "fincontrol-lancamentos" "jwt"

# Plugin: Rate limiting — proteção contra flood no serviço de escrita
kong_upsert_plugin "services" "fincontrol-lancamentos" "rate-limiting" \
  --data "config.minute=300" \
  --data "config.policy=local" \
  --data "config.fault_tolerant=true" \
  --data "config.hide_client_headers=false"

# Plugin: Correlation ID — propaga X-Correlation-Id para rastreamento distribuído
kong_upsert_plugin "services" "fincontrol-lancamentos" "correlation-id" \
  --data "config.header_name=X-Correlation-Id" \
  --data "config.generator=uuid#counter" \
  --data "config.echo_downstream=true"

# Plugin: Request Size Limiting — bloqueia payloads acima de 1MB
kong_upsert_plugin "services" "fincontrol-lancamentos" "request-size-limiting" \
  --data "config.allowed_payload_size=1"

echo ""

# ─── 2. SERVICE: fincontrol-consolidados ────────────────────────────────────
# GET /consolidados/saldo?data-lancamento=YYYY-MM-DD
# Porta: 5260 (launchSettings.json / Consolidado.API)
# NFR: 50 req/s com no máximo 5% de perda

echo "▶ [2/2] Service: fincontrol-consolidados (porta 5260)"
kong_upsert_service \
  "fincontrol-consolidados" \
  "http://host.docker.internal:5260"

echo ""

# Route principal — cobre GET /consolidados/saldo e futuras rotas sob /consolidados
kong_upsert_route \
  "fincontrol-consolidados" \
  "consolidados-saldo" \
  "/consolidados" \
  "GET,OPTIONS"

echo ""

# Plugin: JWT Bearer
kong_upsert_plugin "services" "fincontrol-consolidados" "jwt"

# Plugin: Rate limiting — NFR: 50 req/s, máx 5% perda
#   50 req/s → 3.000 req/min → configuramos headroom de 10% → 3.300/min
#   limit by: consumer (usuário autenticado) para granularidade fina
kong_upsert_plugin "services" "fincontrol-consolidados" "rate-limiting" \
  --data "config.second=55" \
  --data "config.minute=3300" \
  --data "config.policy=local" \
  --data "config.limit_by=consumer" \
  --data "config.fault_tolerant=true" \
  --data "config.hide_client_headers=false"

# Plugin: Proxy Cache — cache de respostas GET por 30s
#   Reduz carga no upstream em picos; saldo consolidado é eventual consistent por design
kong_upsert_plugin "services" "fincontrol-consolidados" "proxy-cache" \
  --data "config.response_code[]=200" \
  --data "config.request_method[]=GET" \
  --data "config.content_type[]=application/json" \
  --data "config.cache_ttl=30" \
  --data "config.strategy=memory"

# Plugin: Correlation ID
kong_upsert_plugin "services" "fincontrol-consolidados" "correlation-id" \
  --data "config.header_name=X-Correlation-Id" \
  --data "config.generator=uuid#counter" \
  --data "config.echo_downstream=true"

echo ""

# ─── 3. CONSUMERS + KEY-AUTH ─────────────────────────────────────────────────
# Cada consumer representa um cliente autorizado a consumir a API.
# A subscription key é validada pelo key-auth plugin ANTES do JWT.
# Fluxo: Kong recebe request → valida X-Subscription-Key → valida JWT Bearer → upstream

echo "▶ [3/3] Consumers e Subscription Keys (key-auth)"
echo ""

# Consumer dedicado para a API de Lançamentos
kong_upsert_consumer \
  "fincontrol-lancamentos-consumer" \
  "$LANC_SUBSCRIPTION_KEY"

echo ""

# Consumer dedicado para a API de Consolidados
kong_upsert_consumer \
  "fincontrol-consolidados-consumer" \
  "$CONS_SUBSCRIPTION_KEY"

echo ""

# Plugin key-auth no service Lancamentos
# Header: X-Subscription-Key (padronizado com o Azure APIM)
# hide_credentials=true → remove o header antes de repassar ao upstream
kong_upsert_plugin "services" "fincontrol-lancamentos" "key-auth" \
  --data "config.key_names[]=X-Subscription-Key" \
  --data "config.hide_credentials=true" \
  --data "config.key_in_header=true" \
  --data "config.key_in_query=false" \
  --data "config.key_in_body=false"

# Plugin key-auth no service Consolidados
kong_upsert_plugin "services" "fincontrol-consolidados" "key-auth" \
  --data "config.key_names[]=X-Subscription-Key" \
  --data "config.hide_credentials=true" \
  --data "config.key_in_header=true" \
  --data "config.key_in_query=false" \
  --data "config.key_in_body=false"

echo ""

# ─── Resumo ──────────────────────────────────────────────────────────────────

echo ""
echo "═══════════════════════════════════════════════════════════"
echo "  ✅ Kong configurado com sucesso!"
echo "═══════════════════════════════════════════════════════════"
echo ""
echo "  SERVICES E ROTAS:"
echo ""
echo "  [Lancamentos — escrita]"
echo "    POST  http://localhost:8000/lancamentos/registrar"
echo "    → upstream: host.docker.internal:5083"
echo "    → plugins: jwt, rate-limiting (300 req/min), correlation-id, request-size-limiting"
echo ""
echo "  [Consolidados — leitura]"
echo "    GET   http://localhost:8000/consolidados/saldo?data-lancamento=YYYY-MM-DD"
echo "    → upstream: host.docker.internal:5260"
echo "    → plugins: jwt, rate-limiting (55 req/s / 3300 req/min), proxy-cache (30s), correlation-id"
echo ""
echo "  [Health checks — acesso direto, sem passar pelo Kong]"
echo "    GET   http://localhost:5083/health        Lancamentos liveness"
echo "    GET   http://localhost:5083/health/ready  Lancamentos readiness"
echo "    GET   http://localhost:5260/health        Consolidados liveness"
echo "    GET   http://localhost:5260/health/ready  Consolidados readiness"
echo ""
echo "  INTERFACES DE ADMINISTRAÇÃO:"
echo "    Kong Manager:    http://localhost:8002"
echo "    Keycloak Admin:  http://localhost:8081/admin  (admin / fincontrol_keycloak_password_123)"
echo ""
echo "  Para autenticar, obtenha um token JWT do Keycloak:"
echo "    POST http://localhost:8081/realms/fincontrol/protocol/openid-connect/token"
echo "    e envie como: Authorization: Bearer <token>"
echo ""
