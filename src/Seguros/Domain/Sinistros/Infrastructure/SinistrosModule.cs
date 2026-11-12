using Seguros.Common.Http;
using Seguros.Domain.Sinistros.Aggregates.Sinistros;
using Seguros.Domain.Sinistros.Infrastructure.ApolicesApi;

namespace Seguros.Domain.Sinistros.Infrastructure;

/// <summary>
/// Registro das dependências do contexto de Sinistros, incluindo o cliente HTTP que ele usa para
/// falar com o contexto de Apólices.
/// </summary>
public static class SinistrosModule
{
    public static IServiceCollection AddSinistrosModule(this IServiceCollection services)
    {
        // Repositório em memória: precisa viver enquanto a aplicação viver.
        services.AddSingleton<ISinistroRepository, InMemorySinistroRepository>();

        services.AddScoped<Features.RegistrarSinistro.Handler>();
        services.AddScoped<Features.ConsultarSinistro.Handler>();
        services.AddScoped<Features.ListarSinistrosPorApolice.Handler>();
        services.AddScoped<Features.ConsultarApoliceDoSinistro.Handler>();

        // Sinistros -> Apólices: comunicação por HTTP, mesmo estando no mesmo processo.
        services.AddContextHttpClient<IApolicesApiClient, ApolicesApiClient>(
            nomeDoPipeline: "sinistros-para-apolices",
            enderecoDoContexto: s => s.ApolicesUri);

        return services;
    }
}
