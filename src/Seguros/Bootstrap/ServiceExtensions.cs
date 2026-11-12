using FastEndpoints;
using FastEndpoints.OpenApi;
using Seguros.Common.Settings;

namespace Seguros.Bootstrap;

internal static class ServiceExtensions
{
    public static IServiceCollection AddDocumentation(this IServiceCollection services)
    {
        services.AddFastEndpoints().OpenApiDocument(o => o.DocumentName = "v1");
        return services;
    }

    /// <summary>
    /// Registra as configurações compartilhadas: o endereço HTTP de cada contexto
    /// (<see cref="ServicesSettings"/>) e os parâmetros de resiliência das chamadas entre
    /// contextos (<see cref="HttpResilienceSettings"/>).
    /// </summary>
    public static IServiceCollection AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<ServicesSettings>()
            .Bind(configuration.GetSection(ServicesSettings.SectionName))
            .Validate(
                s => ServicesSettings.EhUrlHttpAbsoluta(s.ApolicesUri),
                $"'{ServicesSettings.SectionName}:{nameof(ServicesSettings.ApolicesUri)}' precisa ser uma URL http(s) absoluta. " +
                "Defina-a no appsettings.{ASPNETCORE_ENVIRONMENT}.json do ambiente ou na variável " +
                "de ambiente 'ServicesSettings__ApolicesUri'.")
            // Falha já na subida da aplicação, e não na primeira requisição.
            .ValidateOnStart();

        services
            .AddOptions<HttpResilienceSettings>()
            .Bind(configuration.GetSection(HttpResilienceSettings.SectionName));

        return services;
    }
}
