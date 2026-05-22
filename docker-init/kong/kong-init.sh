#!/bin/bash
set -e

echo "⏳ Aguardando Kong ficar pronto..."
until curl -s http://kong:8001/status > /dev/null; do
  echo "  Kong ainda não está pronto..."
  sleep 2
done

echo "✅ Kong pronto!"

echo "⏳ Aguardando Keycloak ficar pronto..."
until curl -s -f -o /dev/null http://keycloak:8080/realms/master; do
  echo "  Keycloak ainda não está pronto..."
  sleep 2
done

echo "✅ Keycloak pronto!"
sleep 5

# Configurar OIDC Realm no Keycloak (opcional - pode ser feito manualmente via UI)
echo "📋 Configurando Kong com Keycloak OIDC..."

# 1. Criar um Kong Service (API de exemplo)
echo "  1️⃣ Criando Kong Service..."
curl -i -X POST http://kong:8001/services \
  --data name=fincontrol-api \
  --data url=http://fincontrol-api:5000 \
  --data protocol=http || echo "Service já existe"

echo ""

# 2. Criar um Kong Route
echo "  2️⃣ Criando Kong Route..."
curl -i -X POST http://kong:8001/services/fincontrol-api/routes \
  --data "hosts[]=api.localhost" \
  --data "paths[]=/api" \
  --data name=fincontrol-api-route || echo "Route já existe"

echo ""

# 3. Configurar OIDC Plugin no Route
echo "  3️⃣ Configurando OIDC Plugin..."
curl -i -X POST http://kong:8001/routes/fincontrol-api-route/plugins \
  --data name=oidc \
  --data config.client_id=kong-client \
  --data config.client_secret=kong-secret \
  --data config.discovery="http://keycloak:8080/realms/fincontrol/.well-known/openid-configuration" \
  --data config.redirect_uri="http://localhost:8000/api/" \
  --data config.scope="openid profile email" \
  --data config.session_secret="fincontrol-kong-session-secret-12345" \
  --data config.token_endpoint_auth_method=client_secret_basic \
  --data config.introspection_endpoint_auth_method=client_secret_basic || echo "OIDC Plugin já configurado"

echo ""
echo "✅ Kong configurado com sucesso!"
echo ""
echo "📌 Próximos passos:"
echo "   1. Acessar http://localhost:8080/admin (Keycloak Admin Console)"
echo "   2. Login: admin / fincontrol_keycloak_password_123"
echo "   3. Criar realm 'fincontrol'"
echo "   4. Criar cliente 'kong-client' com secret 'kong-secret'"
echo "   5. Configurar redirect URIs: http://localhost:8000/api/"
echo "   6. Acessar http://localhost:8002 (Kong Manager) para gerenciar"
echo ""
