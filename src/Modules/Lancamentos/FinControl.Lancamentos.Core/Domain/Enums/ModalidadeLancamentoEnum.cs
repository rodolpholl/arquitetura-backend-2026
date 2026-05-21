using System.ComponentModel.DataAnnotations;

namespace FinControl.Lancamentos.Core.Domain.Enums;

public enum ModalidadeLancamento
{
    [Display(Name = "Venda")]
    Venda = 1,

    [Display(Name = "Devolução")]
    Devolucao = 2,

    [Display(Name = "Suprimento de Caixa")]
    Suprimento = 3,

    [Display(Name = "Sangria de Caixa")]
    Sangria = 4,

    [Display(Name = "Pagamento a Fornecedor")]
    PagamentoFornecedor = 5,

    [Display(Name = "Recebimento de Dívida")]
    RecebimentoDivida = 6,

    [Display(Name = "Outros")]
    Outros = 7
}
