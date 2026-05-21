using System.ComponentModel.DataAnnotations;

namespace FinControl.Lancamentos.Core.Domain.Enums
{
    public enum TipoLancamento
    {
        [Display(Name = "Débito")]
        Debito = 1,
        [Display(Name = "Crédito")]
        Credito = 2
    }
}