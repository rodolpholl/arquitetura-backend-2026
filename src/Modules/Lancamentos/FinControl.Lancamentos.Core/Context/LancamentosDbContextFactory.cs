using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinControl.Lancamentos.Core.Context;

public class LancamentosDbContextFactory : IDesignTimeDbContextFactory<LancamentosDbContext>
{
    public LancamentosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LancamentosDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=fincontrol_lancamentos;Username=fincontrol_admin;Password=fincontrol_dev_password_123")
            .Options;

        return new LancamentosDbContext(options);
    }
}
