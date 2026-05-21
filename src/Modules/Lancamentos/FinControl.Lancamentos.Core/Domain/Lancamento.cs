using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FinControl.Lancamentos.Core.Domain.Enums;
using FinControl.SharedKernel.Domain;

namespace FinControl.Lancamentos.Core.Domain;

public class Lancamento : DomainEntity<long>, IAuditableDomainEntity, ISoftDeleteDomainEntity
{
    /// <summary>
    /// Construtor privado para Entity Framework.
    /// Use <see cref="Criar"/> para instanciar via fábrica.
    /// </summary>
    private Lancamento() { }

    /// <summary>
    /// Fábrica para criar uma nova entidade Lancamento com valores iniciais.
    /// </summary>
    public static Lancamento Criar(
        ModalidadeLancamento modalidade,
        long valor,
        string? descricao = null,
        DateTimeOffset? dataLancamento = null)
    {
        return new Lancamento
        {
            Modalidade = modalidade,
            Valor = valor,
            Descricao = descricao ?? string.Empty,
            DataLancamento = dataLancamento ?? DateTimeOffset.UtcNow
        };
    }

    public ModalidadeLancamento Modalidade { get; private set; }
    public long Valor { get; set; }
    public decimal ValorFormatado => Valor / 100m;
    public TipoLancamento Tipo => Valor < 0 ? TipoLancamento.Debito : TipoLancamento.Credito;
    public string? TipoFormatado => Tipo.GetType()
        .GetField(Tipo.ToString())
        ?.GetCustomAttribute<DisplayAttribute>()?.Name ?? Tipo.ToString();
    public string? Descricao { get; private set; } = string.Empty;
    public DateTimeOffset DataLancamento { get; private set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public string CreatedByEmail { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
    public string? UpdatedByName { get; set; }
    public string? UpdatedByEmail { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
