using FinControl.Infrastructure.Http;
using FinControl.Lancamentos.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;

/// <summary>
/// Request DTO para o endpoint POST /lancamentos/registrar.
/// Estrutura de entrada do cliente (sem informações de auditoria que vêm do contexto).
/// </summary>
public record RegistrarLancamentoRequest(
    /// <summary>
    /// Modalidade do lançamento.
    /// </summary>
    ModalidadeLancamento Modalidade,

    /// <summary>
    /// Valor em centavos.
    /// </summary>
    long Valor,

    /// <summary>
    /// Descrição opcional.
    /// </summary>
    string? Descricao = null,

    /// <summary>
    /// Data do lançamento (opcional, usa UTC now se omitida).
    /// Aceita formatos: "2024-05-22T10:30:00Z", "2024-05-22T10:30:00+00:00", "2024-05-22T10:30:00-03:00".
    /// </summary>
    DateTimeOffset? DataLancamento = null
);

/// <summary>
/// Endpoint HTTP para registrar um novo lançamento.
/// Implements Vertical Slicing com Wolverine minimal APIs.
/// Extrai dados do usuário diretamente do JWT do Keycloak usando utilitários da Infrastructure.
/// 
/// Padrão Wolverine: O método "Handle" com atributo [WolverinePost] é descoberto automaticamente.
/// </summary>
public class RegistrarLancamentoEndpoint
{
    /// <summary>
    /// Handler do endpoint - descoberto automaticamente pelo Wolverine.
    /// </summary>
    [Authorize]
    [WolverinePost("/lancamentos/registrar")]
    public static async Task<RegistrarLancamentoResponse> Handle(
        RegistrarLancamentoRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken = default)
    {
        // 1. Validar autenticação
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("Usuário não autenticado. Token JWT inválido ou expirado.");
        }

        // 2. Extrair dados do usuário e rastreamento usando extensões da Infrastructure
        var (usuarioId, usuarioNome, usuarioEmail) = httpContext.ExtrairDadosUsuario();
        var correlationId = httpContext.ExtrairCorrelationId();
        var idempotencyKey = httpContext.ExtrairIdempotencyKey();

        // 3. Construir command com dados do contexto
        var command = new RegistrarLancamentoCommand
        {
            Modalidade = request.Modalidade,
            Valor = request.Valor,
            Descricao = request.Descricao,
            DataLancamento = request.DataLancamento?.ToUniversalTime() ?? DateTimeOffset.UtcNow,
            UsuarioId = usuarioId,
            UsuarioNome = usuarioNome,
            UsuarioEmail = usuarioEmail,
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey,
        };

        // 4. Enviar command via Wolverine (mediator)
        var response = await bus.InvokeAsync<RegistrarLancamentoResponse>(command, cancellationToken);

        // 5. Adicionar headers de rastreamento na resposta
        httpContext.AdicionarHeadersRastreamento(correlationId, idempotencyKey);

        return response;
    }
}
