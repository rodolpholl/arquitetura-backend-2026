namespace FinControl.Infrastructure.Data;

/// <summary>
/// Entidades que implementam esta interface recebem preenchimento
/// automático de CriadoEm e AtualizadoEm no BaseDbContext.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CriadoEm { get; set; }
    DateTimeOffset? AtualizadoEm { get; set; }
}
