using FinControl.Consolidado.Core.Features.Queries.GetSaldoConsolidado;
using FinControl.Infrastructure.Extensions;
using Microsoft.AspNetCore.Builder;

namespace FinControl.Consolidado.Core.Features;

public static class ConsolidadosFeatureExtensions
{
    public static WebApplicationBuilder AddConsolidadosFeatures(
        this WebApplicationBuilder builder)
    {
        builder.AddFinControlRedis();

        builder.AddFinControlWolverine(
            configure: null,
            typeof(GetSaldoConsolidadoQueryHandler).Assembly);

        return builder;
    }

    public static WebApplication MapConsolidadosMiddleware(this WebApplication app)
    {
        app.UseFinControlMiddleware();
        return app;
    }
}
