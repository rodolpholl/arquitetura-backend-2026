using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace FinControl.Infrastructure.Wolverine;

/// <summary>
/// Middleware Wolverine de validação automática via FluentValidation.
///
/// Executa ANTES do handler (método Before), seguindo o pipeline do mediator do Wolverine.
///
/// Fluxo do pipeline para cada Command/Query:
///   LoggingMiddleware.Before → FluentValidationMiddleware.Before → Handler → LoggingMiddleware.After
///
/// Se a validação falhar:
///   - Lança ValidationException (FluentValidation)
///   - GlobalExceptionHandler intercepta e retorna 400 ProblemDetails com os erros
///   - O handler NUNCA é executado
///
/// Para ativar globalmente no WolverineOptions:
///   opts.Policies.AddMiddleware(typeof(FluentValidationMiddleware));
///
/// Para registrar validators no DI:
///   services.AddValidatorsFromAssembly(typeof(MinhaFeatureAssembly).Assembly);
/// </summary>
public sealed class FluentValidationMiddleware
{
    /// <summary>
    /// Wolverine injeta todos os parâmetros do método Before via IoC.
    /// IServiceProvider é usado para resolver IValidator&lt;T&gt; opcionalmente
    /// (sem lançar exceção quando não existe validator para o tipo).
    ///
    /// Convenção Wolverine: um Before que lança exceção INTERROMPE o pipeline.
    /// Não é necessário retornar HandlerContinuation — basta lançar ou retornar.
    /// </summary>
    public async Task Before<T>(
        T message,
        IServiceProvider services,
        ILogger<FluentValidationMiddleware> logger,
        CancellationToken ct) where T : class
    {
        // Resolução opcional — sem validator registrado, segue o pipeline normalmente
        var validator = services.GetService<IValidator<T>>();
        if (validator is null)
            return;

        var result = await validator.ValidateAsync(message, ct);
        if (result.IsValid)
            return;

        // Log estruturado dos erros antes de interromper
        var erros = result.Errors
            .Select(f => $"{f.PropertyName}: {f.ErrorMessage}")
            .ToArray();

        logger.LogWarning(
            "Validação falhou para {MessageType}. Erros: [{Errors}]",
            typeof(T).Name,
            string.Join(" | ", erros));

        // Lança ValidationException — GlobalExceptionHandler mapeia para 400 ProblemDetails
        throw new ValidationException(result.Errors);
    }
}
