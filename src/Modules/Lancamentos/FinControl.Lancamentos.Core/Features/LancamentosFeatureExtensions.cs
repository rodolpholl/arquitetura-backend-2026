using FinControl.Infrastructure.Extensions;
using FinControl.Lancamentos.Core.Context;
using FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FinControl.Lancamentos.Core.Features;

/// <summary>
/// Extensões de DI para registrar handlers, validators e endpoints do módulo Lancamentos.
/// 
/// Padrão: Esta extensão é chamada pelo ModuleExtensions do projeto .API.
/// Integração com Wolverine (auto-descoberta) + FluentValidation.
/// </summary>
public static class LancamentosFeatureExtensions
{
    /// <summary>
    /// Registra todos os handlers, validators e endpoints do módulo Lancamentos.
    /// 
    /// Wolverine usa descoberta por convenção (método "Handle" ou "HandleAsync").
    /// Validators são descobertos como AbstractValidator<T>.
    /// </summary>
    public static WebApplicationBuilder AddLancamentosFeatures(
        this WebApplicationBuilder builder)
    {
        // Registrar Wolverine com descoberta automática de handlers no assembly do Lancamentos.Core
        builder.AddFinControlWolverine<LancamentosDbContext>(
            configure: null,
            // Assembly onde Wolverine descobrirá handlers por convenção
            typeof(RegistrarLancamentoCommandHandler).Assembly
        );

        // Registrar validators FluentValidation (auto-descoberta por interface AbstractValidator<T>)
        builder.Services.AddValidatorsFromAssemblyContaining<RegistrarLancamentoCommandValidator>(
            lifetime: ServiceLifetime.Transient);

        return builder;
    }

    /// <summary>
    /// Mapeia os middlewares HTTP do FinControl (CorrelationId, ExceptionHandler).
    /// </summary>
    public static WebApplication MapLancamentosMiddleware(this WebApplication app)
    {
        app.UseFinControlMiddleware();
        return app;
    }
}
