using FinControl.Lancamentos.Core.Domain.Enums;
using FluentValidation;

namespace FinControl.Lancamentos.Core.Features.Commands.RegistrarLancamento;

/// <summary>
/// Validator para o comando RegistrarLancamento usando FluentValidation.
/// Aplica validações de negócio e format de dados.
/// </summary>
public class RegistrarLancamentoCommandValidator : AbstractValidator<RegistrarLancamentoCommand>
{
    public RegistrarLancamentoCommandValidator()
    {
        // Modalidade
        RuleFor(x => x.Modalidade)
            .IsInEnum()
            .WithMessage("Modalidade inválida. Deve ser uma das modalidades disponíveis.");

        // Valor
        RuleFor(x => x.Valor)
            .GreaterThan(0)
            .WithMessage("Valor deve ser maior que zero (mínimo 1 centavo).")
            .LessThanOrEqualTo(99999999999) // ~999.999.999,99
            .WithMessage("Valor máximo permitido é R$ 999.999.999,99.")
            
            .Must((model, v) => model.Modalidade == ModalidadeLancamento.Venda ||
                                model.Modalidade == ModalidadeLancamento.RecebimentoDivida ||
                                model.Modalidade == ModalidadeLancamento.Devolucao ? v >= 0 : true)
            .WithMessage("Valor deve ser maior que zero para modalidades informada.")

            .Must((model, v) => model.Modalidade == ModalidadeLancamento.Suprimento ||
                                model.Modalidade == ModalidadeLancamento.PagamentoFornecedor ||
                                model.Modalidade == ModalidadeLancamento.Sangria ? v <0 : true)
            .WithMessage("Valor deve ser menor que zero para modalidades informada.");

        

        // Descrição - obrigatória se Modalidade = Outros
        RuleFor(x => x.Descricao)
            .NotEmpty()
            .When(x => x.Modalidade == ModalidadeLancamento.Outros)
            .WithMessage("Descrição é obrigatória para lançamentos do tipo 'Outros'.")
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Descricao))
            .WithMessage("Descrição não pode exceder 500 caracteres.");

        // Data do Lançamento
        RuleFor(x => x.DataLancamento)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow.AddDays(1))
            .WithMessage("Data do lançamento não pode ser no futuro (máximo 1 dia a frente).");

        RuleFor(x => x.DataLancamento)
            .GreaterThanOrEqualTo(DateTimeOffset.UtcNow.AddYears(-1))
            .WithMessage("Data do lançamento não pode ser anterior a 1 ano.")
            .When(x => x.DataLancamento != default)
            .WithMessage("Data do lançamento é obrigatória.");

        // UsuarioId
        RuleFor(x => x.UsuarioId)
            .NotEmpty()
            .WithMessage("ID do usuário é obrigatório.");

        RuleFor(x => x.UsuarioId)
            .Length(36) // UUID format
            .When(x => !string.IsNullOrWhiteSpace(x.UsuarioId))
            .WithMessage("ID do usuário deve ser um UUID válido.");

        // UsuarioNome
        RuleFor(x => x.UsuarioNome)
            .NotEmpty()
            .WithMessage("Nome do usuário é obrigatório.")
            .MaximumLength(200)
            .WithMessage("Nome do usuário não pode exceder 200 caracteres.");

        // UsuarioEmail
        RuleFor(x => x.UsuarioEmail)
            .NotEmpty()
            .WithMessage("Email do usuário é obrigatório.")
            .EmailAddress()
            .WithMessage("Email do usuário deve ser um endereço válido.")
            .MaximumLength(200)
            .WithMessage("Email não pode exceder 200 caracteres.");

        // IdempotencyKey
        RuleFor(x => x.IdempotencyKey)
            .NotEqual(Guid.Empty)
            .WithMessage("Chave de idempotência inválida.");

        // CorrelationId
        RuleFor(x => x.CorrelationId)
            .NotEqual(Guid.Empty)
            .WithMessage("ID de correlação inválido.");
    }
}
