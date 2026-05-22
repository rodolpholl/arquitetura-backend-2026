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
    public ModalidadeLancamento Modalidade { get; set; }
    public long Valor { get; set; }
    public decimal ValorFormatado => Valor / 100m;
    public TipoLancamento Tipo => Valor < 0 ? TipoLancamento.Debito : TipoLancamento.Credito;
    public string? TipoFormatado => Tipo.GetType()
        .GetField(Tipo.ToString())
        ?.GetCustomAttribute<DisplayAttribute>()?.Name ?? Tipo.ToString();
    public string? Descricao { get; set; } = string.Empty;
    public DateTimeOffset DataLancamento { get; set; }

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
