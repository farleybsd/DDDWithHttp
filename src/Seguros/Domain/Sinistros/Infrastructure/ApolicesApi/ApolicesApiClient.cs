using System.Net;
using System.Net.Http.Json;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;
using Seguros.Domain.Sinistros.Aggregates.Sinistros;

namespace Seguros.Domain.Sinistros.Infrastructure.ApolicesApi;

/// <summary>
/// Adaptador HTTP da porta <see cref="IApolicesApiClient"/>. Chama os endpoints públicos do
/// contexto de Apólices por HTTP — mesmo os dois contextos rodando no mesmo processo — através
/// do HttpClient tipado registrado em <c>SinistrosModule.AddSinistrosModule</c> (timeout, retry
/// e circuit breaker aplicados pelo pipeline do Microsoft.Extensions.Http.Resilience).
/// </summary>
/// <remarks>
/// Este é o único ponto de contato entre os contextos: nenhum tipo de
/// <c>Seguros.Domain.Apolices</c> é referenciado aqui, só o contrato JSON (<see cref="ApoliceApiDto"/>).
/// </remarks>
public sealed class ApolicesApiClient(HttpClient httpClient, ILogger<ApolicesApiClient> logger) : IApolicesApiClient
{
    public async Task<ApoliceConsultaResultado> ConsultarApoliceAsync(Guid apoliceId, CancellationToken cancellationToken)
    {
        try
        {
            // Rota relativa (sem barra inicial): o endereço base vem do ambiente atual
            // (ServicesSettings.ApolicesUri) e pode conter um caminho, que seria descartado por uma
            // barra inicial aqui.
            using var response = await httpClient.GetAsync($"apolices/{apoliceId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new ApoliceConsultaResultado.NaoEncontrada();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Consulta à API de Apólices retornou {StatusCode} para a apólice {ApoliceId}.",
                    (int)response.StatusCode,
                    apoliceId);

                return new ApoliceConsultaResultado.Indisponivel($"Resposta inesperada da API de Apólices: {(int)response.StatusCode}.");
            }

            var dto = await response.Content.ReadFromJsonAsync<ApoliceApiDto>(cancellationToken);
            if (dto is null)
                return new ApoliceConsultaResultado.Indisponivel("Resposta vazia da API de Apólices.");

            return new ApoliceConsultaResultado.Encontrada(Traduzir(dto));
        }
        catch (TimeoutRejectedException)
        {
            logger.LogWarning("Tempo limite excedido ao consultar a apólice {ApoliceId} na API de Apólices.", apoliceId);
            return new ApoliceConsultaResultado.Indisponivel("Tempo limite excedido ao consultar a API de Apólices.");
        }
        catch (RateLimiterRejectedException)
        {
            logger.LogWarning("Limite de chamadas simultâneas ao contexto de Apólices atingido — requisição para a apólice {ApoliceId} recusada.", apoliceId);
            return new ApoliceConsultaResultado.Indisponivel("Limite de chamadas simultâneas à API de Apólices atingido.");
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning("Circuito aberto para a API de Apólices — requisição para a apólice {ApoliceId} não foi enviada.", apoliceId);
            return new ApoliceConsultaResultado.Indisponivel("API de Apólices temporariamente indisponível (circuito aberto).");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Falha de comunicação com a API de Apólices ao consultar a apólice {ApoliceId}.", apoliceId);
            return new ApoliceConsultaResultado.Indisponivel("Falha de comunicação com a API de Apólices.");
        }
    }

    private static ApoliceReferenciada Traduzir(ApoliceApiDto dto) => new(
        dto.Id,
        dto.Vigencia.Inicio,
        dto.Vigencia.Fim,
        dto.Coberturas.Select(c => Enum.Parse<TipoCobertura>(c.Tipo, ignoreCase: true)).ToList());
}
