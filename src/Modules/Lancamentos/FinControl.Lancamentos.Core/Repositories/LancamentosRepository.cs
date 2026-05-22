using FinControl.Infrastructure.Repositories;
using FinControl.Lancamentos.Core.Context;

namespace FinControl.Lancamentos.Core.Repositories;

/// <summary>
/// Repositório genérico concreto para entidades do módulo Lancamentos.
/// Especializa Repository&lt;TEntity&gt; injetando LancamentosDbContext.
/// </summary>
public class LancamentosRepository<TEntity> : Repository<TEntity>
    where TEntity : class
{
    public LancamentosRepository(LancamentosDbContext context) : base(context)
    {
    }
}
