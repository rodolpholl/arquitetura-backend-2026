-- Script de inicialização do PostgreSQL
-- Cria databases e schemas para diferentes contextos da aplicação
-- Este script é idempotente (seguro rodar múltiplas vezes)

-- Database para Lançamentos (débitos e créditos)
SELECT 'CREATE DATABASE agile_lancamentos WITH ENCODING UTF8 LC_COLLATE en_US.UTF-8 LC_CTYPE en_US.UTF-8 TEMPLATE template0'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'agile_lancamentos')\gexec

-- Database para Consolidado (diário, mensal)
SELECT 'CREATE DATABASE agile_consolidado WITH ENCODING UTF8 LC_COLLATE en_US.UTF-8 LC_CTYPE en_US.UTF-8 TEMPLATE template0'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'agile_consolidado')\gexec

-- Database para Outbox Pattern (garantia de entrega de eventos)
SELECT 'CREATE DATABASE agile_outbox WITH ENCODING UTF8 LC_COLLATE en_US.UTF-8 LC_CTYPE en_US.UTF-8 TEMPLATE template0'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'agile_outbox')\gexec

-- Conectar ao database agile_lancamentos para criar schemas
\c agile_lancamentos

-- Schema para domínio de Lançamentos
CREATE SCHEMA IF NOT EXISTS lancamentos
  AUTHORIZATION agile_admin;

-- Schema para infraestrutura (auditoria, logs)
CREATE SCHEMA IF NOT EXISTS infraestrutura
  AUTHORIZATION agile_admin;

-- Conectar ao database agile_consolidado
\c agile_consolidado

-- Schema para Consolidado Diário
CREATE SCHEMA IF NOT EXISTS consolidado_diario
  AUTHORIZATION agile_admin;

-- Schema para Consolidado Mensal
CREATE SCHEMA IF NOT EXISTS consolidado_mensal
  AUTHORIZATION agile_admin;

-- Conectar ao database agile_outbox
\c agile_outbox

-- Schema para Outbox (garantia de entrega)
CREATE SCHEMA IF NOT EXISTS outbox
  AUTHORIZATION agile_admin;

-- Schema para Inbox (deduplicação)
CREATE SCHEMA IF NOT EXISTS inbox
  AUTHORIZATION agile_admin;

-- Permissões
GRANT CONNECT ON DATABASE agile_lancamentos TO agile_admin;
GRANT CONNECT ON DATABASE agile_consolidado TO agile_admin;
GRANT CONNECT ON DATABASE agile_outbox TO agile_admin;

GRANT USAGE ON SCHEMA lancamentos TO agile_admin;
GRANT USAGE ON SCHEMA infraestrutura TO agile_admin;
GRANT USAGE ON SCHEMA consolidado_diario TO agile_admin;
GRANT USAGE ON SCHEMA consolidado_mensal TO agile_admin;
GRANT USAGE ON SCHEMA outbox TO agile_admin;
GRANT USAGE ON SCHEMA inbox TO agile_admin;

GRANT CREATE ON SCHEMA lancamentos TO agile_admin;
GRANT CREATE ON SCHEMA infraestrutura TO agile_admin;
GRANT CREATE ON SCHEMA consolidado_diario TO agile_admin;
GRANT CREATE ON SCHEMA consolidado_mensal TO agile_admin;
GRANT CREATE ON SCHEMA outbox TO agile_admin;
GRANT CREATE ON SCHEMA inbox TO agile_admin;

-- Exibir databases e schemas criados
\l
