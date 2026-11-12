using Seguros.Domain.Apolices.Aggregates.Apolices;

namespace Seguros.Domain.Apolices.Infrastructure;

/// <summary>
/// Registro das dependências do contexto de Apólices. Este contexto não conhece nenhum outro.
/// </summary>
public static class ApolicesModule
{
    public static IServiceCollection AddApolicesModule(this IServiceCollection services)
    {
        // Repositório em memória: precisa viver enquanto a aplicação viver.
        services.AddSingleton<IApoliceRepository, InMemoryApoliceRepository>();

        services.AddScoped<Features.ConsultarApolice.Handler>();
        services.AddScoped<Features.ListarApolices.Handler>();

        return services;
    }
}
