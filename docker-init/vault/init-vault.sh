#!/bin/bash

################################################################################
# Inicializa o Vault com secrets de desenvolvimento para Agile Workers
#
# Uso: ./init-vault.sh [--env dev|staging|production]
# 
# Variáveis de ambiente:
#   VAULT_ADDR      - Endereço do Vault (padrão: http://vault:8200)
#   VAULT_TOKEN     - Token de acesso (padrão: agile_dev_token_12345)
#   VAULT_ENV       - Ambiente: dev, staging, production (padrão: dev)
################################################################################

set -euo pipefail

# Configurações padrão
VAULT_ADDR="${VAULT_ADDR:-http://vault:8200}"
VAULT_TOKEN="${VAULT_TOKEN:-agile_dev_token_12345}"
VAULT_ENV="${VAULT_ENV:-dev}"

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

################################################################################
# Funções Auxiliares
################################################################################

print_header() {
    echo -e "\n${CYAN}════════════════════════════════════════════════════════${NC}"
    echo -e "${CYAN}  $1${NC}"
    echo -e "${CYAN}════════════════════════════════════════════════════════${NC}\n"
}

print_info() {
    echo -e "${CYAN}ℹ${NC}  $1"
}

print_success() {
    echo -e "${GREEN}✓${NC}  $1"
}

print_error() {
    echo -e "${RED}✗${NC}  $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC}  $1"
}

################################################################################
# Testa Conexão com Vault
################################################################################

test_vault_connection() {
    print_info "Testando conexão com Vault..."
    
    if ! response=$(curl -s -f "${VAULT_ADDR}/v1/sys/health" 2>/dev/null); then
        print_error "Erro ao conectar ao Vault em ${VAULT_ADDR}"
        echo -e "${YELLOW}💡 Dica: Verifique se o Vault está rodando e accessible${NC}\n"
        return 1
    fi
    
    print_success "Vault está acessível!"
    return 0
}

################################################################################
# Cria um Secret no Vault
################################################################################

create_secret() {
    local secret_name="$1"
    shift
    local -a secret_data=("$@")
    
    # Monta o JSON dinamicamente
    local json="{\"data\": {"
    local first=true
    
    for pair in "${secret_data[@]}"; do
        IFS='=' read -r key value <<< "$pair"
        if [ "$first" = true ]; then
            json="${json}\"${key}\": \"${value}\""
            first=false
        else
            json="${json}, \"${key}\": \"${value}\""
        fi
    done
    
    json="${json}}}"
    
    print_info "Criando secret: ${secret_name}"
    
    local secret_path="secret/data/${VAULT_ENV}/${secret_name}"
    local url="${VAULT_ADDR}/v1/${secret_path}"
    
    local response
    response=$(curl -s -w "\n%{http_code}" -X POST \
        -H "X-Vault-Token: ${VAULT_TOKEN}" \
        -H "Content-Type: application/json" \
        -d "${json}" \
        "${url}" 2>&1)
    
    local http_code
    http_code=$(echo "${response}" | tail -1)
    local body
    body=$(echo "${response}" | head -1)
    
    if [ "${http_code}" = "200" ] || [ "${http_code}" = "204" ]; then
        print_success "Secret '${secret_name}' criado (HTTP ${http_code})"
        return 0
    else
        print_error "Falha ao criar '${secret_name}' (HTTP ${http_code}): ${body}"
        return 1
    fi
}

################################################################################
# Lê um Secret do Vault
################################################################################

read_secret() {
    local secret_name="$1"
    local secret_path="secret/data/${VAULT_ENV}/${secret_name}"
    
    curl -s -H "X-Vault-Token: ${VAULT_TOKEN}" \
        "${VAULT_ADDR}/v1/${secret_path}" 2>/dev/null || echo "{}"
}

################################################################################
# EXECUÇÃO PRINCIPAL
################################################################################

main() {
    echo ""
    echo -e "${CYAN}🔐 Inicializador de Secrets - Agile Workers Backend${NC}"
    echo ""
    print_info "Configurações:"
    echo -e "   • Vault Address: ${VAULT_ADDR}"
    echo -e "   • Environment: ${VAULT_ENV}"
    echo -e "   • Token: ${VAULT_TOKEN:0:5}...***\n"
    
    # Teste de conexão
    if ! test_vault_connection; then
        exit 1
    fi
    
    # Criar Secrets
    print_header "Criando Secrets no Vault (${VAULT_ENV})"
    
    # PostgreSQL
    echo -e "${CYAN}🗄️  PostgreSQL${NC}"
    create_secret "postgres" \
        "username=agile_admin" \
        "password=agile_dev_password_123" \
        "host=postgres" \
        "port=5432"
    echo ""
    
    # Redis
    echo -e "${CYAN}⚡ Redis${NC}"
    create_secret "redis" \
        "password=agile_redis_password_123" \
        "host=redis" \
        "port=6379"
    echo ""
    
    # RabbitMQ
    echo -e "${CYAN}🐰 RabbitMQ${NC}"
    create_secret "rabbitmq" \
        "username=agile_user" \
        "password=agile_rabbitmq_password_123" \
        "vhost=/agile" \
        "host=rabbitmq" \
        "port=5672"
    echo ""
    
    # Grafana
    echo -e "${CYAN}📊 Grafana${NC}"
    create_secret "grafana" \
        "admin_password=agile_grafana_password_123" \
        "admin_username=admin"
    echo ""
    
    # Vault
    echo -e "${CYAN}🔑 Vault${NC}"
    create_secret "vault" \
        "root_token=agile_dev_token_12345"
    echo ""
    
    # Verificar Secrets Criados
    print_header "Verificando Secrets Criados"
    
    for secret in postgres redis rabbitmq grafana vault; do
        echo -e "${CYAN}📋 Lendo: secret/${VAULT_ENV}/${secret}${NC}"
        
        response=$(read_secret "$secret")
        
        if echo "$response" | grep -q '"data"'; then
            print_success "Encontrado:"
            echo "$response" | grep -o '"[^"]*":"[^"]*"' | head -5 | sed 's/^/   • /' | sed 's/":"/: /' | sed 's/"//g'
        else
            print_error "Secret não encontrado!"
        fi
        echo ""
    done
    
    # Resumo
    print_header "Resumo da Configuração"
    
    print_success "Todos os secrets foram inicializados com sucesso!"
    echo ""
    print_info "Próximos passos:"
    echo -e "   1. Acessar Vault UI: ${VAULT_ADDR}/ui"
    echo -e "   2. Usar token: ${VAULT_TOKEN}"
    echo -e "   3. Navegar para: secret/${VAULT_ENV}/"
    echo ""
    print_warning "LEMBRETE: Os valores acima são APENAS para desenvolvimento!"
    print_warning "          Em produção, gere secrets fortes e únicos!"
    echo ""
}

# Executa
main
