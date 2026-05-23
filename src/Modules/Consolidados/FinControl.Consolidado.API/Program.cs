using FinControl.Auth.Extensions;
using FinControl.Consolidado.API.Configuration;
using FinControl.Infrastructure.Extensions;
using FinControl.Infrastructure.Middleware;
using FinControl.Infrastructure.Vault;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURAÇÃO DE SECRETS (VAULT) ====================
try
{
    builder.AddFinControlVault();
}
catch (Exception ex) when (builder.Environment.IsDevelopment())
{
    Console.WriteLine($"⚠️  Vault não disponível em desenvolvimento: {ex.Message}");
}

// ==================== LOGGING ====================
try
{
    builder.AddFinControlSerilog("fincontrol-consolidados");
}
catch (Exception ex) when (builder.Environment.IsDevelopment())
{
    Console.WriteLine($"⚠️  Serilog não fully configurado em desenvolvimento: {ex.Message}");
}

// ==================== OBSERVABILIDADE ====================
try
{
    builder.AddFinControlObservability("fincontrol-consolidados");
}
catch (Exception ex) when (builder.Environment.IsDevelopment())
{
    Console.WriteLine($"⚠️  Observabilidade não disponível em desenvolvimento: {ex.Message}");
}

// ==================== HEALTH CHECKS ====================
try
{
    builder.Services.AddFinControlHealthChecks(
        builder.Configuration,
        includeRedis: true,
        includeRabbitMq: !builder.Environment.IsDevelopment());
}
catch (Exception ex) when (builder.Environment.IsDevelopment())
{
    Console.WriteLine($"⚠️  Health checks incompletos em desenvolvimento: {ex.Message}");
}

// ==================== AUTENTICAÇÃO JWT (KEYCLOAK) ====================
try
{
    builder.AddFinControlKeycloakAuth();
}
catch (Exception ex) when (builder.Environment.IsDevelopment())
{
    Console.WriteLine($"⚠️  Keycloak não configurado em desenvolvimento: {ex.Message}");
}

// ==================== API / OPENAPI ====================
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// ==================== TODOS OS MÓDULOS DE FEATURES ====================
builder.AddAllModules();

// ==================== EXCEPTION HANDLING ====================
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ==================== BUILD PIPELINE HTTP ====================
var app = builder.Build();

// ==================== MIDDLEWARES HTTP ====================

// 1. Request Logging (Serilog)
try
{
    app.UseFinControlRequestLogging();
}
catch
{
    // Se Serilog não foi configurado, continua sem
}

// 2. HTTPS Redirect
app.UseHttpsRedirection();

// 3. Observabilidade
try
{
    app.UseFinControlObservability();
}
catch
{
    // Se observabilidade não foi configurada, continua sem
}

// 4. Exception Handler Global (RFC 7807 ProblemDetails)
app.UseExceptionHandler();

// 5. Subscription Key (segunda camada após Kong — cobre requisições que bypassam o gateway)
app.UseSubscriptionKeyValidation(VaultKeys.KongConsolidadosSubscriptionKey);

// 6. Autenticação e Autorização
app.UseAuthentication();
app.UseAuthorization();

// ==================== HEALTH CHECKS ====================
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

// ==================== ENDPOINTS ====================
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.AddPreferredSecuritySchemes("Bearer");
    });
}

app.MapAllModules();

// Prometheus /metrics
app.MapFinControlMetricsEndpoint();

// ==================== RUN ====================
Console.WriteLine($"\n🚀 Consolidados iniciando em modo: {(app.Environment.IsDevelopment() ? "DESENVOLVIMENTO" : "PRODUÇÃO")}\n");
app.Run();
