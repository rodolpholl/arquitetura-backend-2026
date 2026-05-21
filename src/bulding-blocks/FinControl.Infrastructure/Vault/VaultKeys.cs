namespace FinControl.Infrastructure.Vault;

/// <summary>
/// Mapa completo de chaves do HashiCorp Vault para o sistema FinControl,
/// alinhado à estrutura real do Vault (mount: <c>secret</c>, prefixo: <c>dev/</c>).
///
/// ────────────────────────────────────────────────────────────────────────────
/// ESTRUTURA REAL DO VAULT (KV v2)
/// ────────────────────────────────────────────────────────────────────────────
///
///   secrets > secret > dev/
///   ├── dev/grafana      → loki_url, otlp_endpoint, prometheus_pushgateway
///   ├── dev/postgres     → connection_string
///   ├── dev/rabbitmq     → uri
///   ├── dev/redis        → connection_string
///   └── dev/vault        → (metadados internos / AppRole credentials)
///
/// ────────────────────────────────────────────────────────────────────────────
/// COMO AS CHAVES SÃO INJETADAS NO IConfiguration
/// ────────────────────────────────────────────────────────────────────────────
///
/// O VaultConfigurationProvider usa o ÚLTIMO SEGMENTO do path como namespace:
///
///   Path "dev/postgres" + key "connection_string"
///   → IConfiguration["postgres:connection_string"]
///
///   Path "dev/grafana" + key "loki_url"
///   → IConfiguration["grafana:loki_url"]
///
/// Acesse sempre via: builder.Configuration[VaultKeys.CONSTANTE]
///
/// ────────────────────────────────────────────────────────────────────────────
/// VAULT SETTINGS (vault.settings.json)
/// ────────────────────────────────────────────────────────────────────────────
/// {
///   "Vault": {
///     "MountPoint": "secret",
///     "SecretPaths": ["dev/postgres", "dev/redis", "dev/rabbitmq", "dev/grafana", "dev/vault"],
///     "ConfigurationPrefix": ""
///   }
/// }
///
/// ────────────────────────────────────────────────────────────────────────────
/// SETUP VAULT CLI (ambiente dev)
/// ────────────────────────────────────────────────────────────────────────────
///   vault kv put secret/dev/postgres   connection_string="Host=postgres;Database=fincontrol;Username=admin;Password=..."
///   vault kv put secret/dev/redis      connection_string="redis:6379,password=..."
///   vault kv put secret/dev/rabbitmq   uri="amqp://user:pass@rabbitmq:5672"
///   vault kv put secret/dev/grafana    loki_url="http://loki:3100" otlp_endpoint="http://tempo:4317" prometheus_pushgateway="http://pushgateway:9091"
///   vault kv put secret/dev/vault      role_id="..." secret_id="..."
/// </summary>
public static class VaultKeys
{
    // ── dev/postgres ─────────────────────────────────────────────────────────

    /// <summary>
    /// Connection string do PostgreSQL principal.
    /// Vault path: <c>dev/postgres</c> → key <c>connection_string</c>
    /// IConfiguration: <c>postgres:connection_string</c>
    /// </summary>
    public const string PostgresConnection = "postgres:connection_string";

    // ── dev/redis ────────────────────────────────────────────────────────────

    /// <summary>
    /// Connection string do Redis (cache distribuído e sessões).
    /// Vault path: <c>dev/redis</c> → key <c>connection_string</c>
    /// IConfiguration: <c>redis:connection_string</c>
    /// </summary>
    public const string RedisConnection = "redis:connection_string";

    // ── dev/rabbitmq ─────────────────────────────────────────────────────────

    /// <summary>
    /// URI de conexão do RabbitMQ. Formato: <c>amqp://user:pass@host:5672/vhost</c>
    /// Vault path: <c>dev/rabbitmq</c> → key <c>uri</c>
    /// IConfiguration: <c>rabbitmq:uri</c>
    /// </summary>
    public const string RabbitMqUri = "rabbitmq:uri";

    // ── dev/grafana ──────────────────────────────────────────────────────────

    /// <summary>
    /// URL do Grafana Loki para push de logs estruturados via Serilog.
    /// Ex: <c>http://loki:3100</c>
    /// Vault path: <c>dev/grafana</c> → key <c>loki_url</c>
    /// IConfiguration: <c>grafana:loki_url</c>
    /// </summary>
    public const string LokiUrl = "grafana:loki_url";

    /// <summary>
    /// Endpoint OTLP gRPC para envio de traces (Grafana Tempo / Jaeger).
    /// Ex: <c>http://tempo:4317</c>
    /// Vault path: <c>dev/grafana</c> → key <c>otlp_endpoint</c>
    /// IConfiguration: <c>grafana:otlp_endpoint</c>
    /// </summary>
    public const string OtlpEndpoint = "grafana:otlp_endpoint";

    /// <summary>
    /// URL do Prometheus Pushgateway (para métricas de workers/jobs).
    /// Ex: <c>http://pushgateway:9091</c>
    /// Vault path: <c>dev/grafana</c> → key <c>prometheus_pushgateway</c>
    /// IConfiguration: <c>grafana:prometheus_pushgateway</c>
    /// </summary>
    public const string PrometheusPushgateway = "grafana:prometheus_pushgateway";

    // ── dev/vault ────────────────────────────────────────────────────────────
    // O path dev/vault contém as credenciais AppRole para renovação automática
    // de tokens em produção. Não exposto como constante pública — lido apenas
    // internamente pelo VaultConfigurationProvider durante o bootstrap.
}

