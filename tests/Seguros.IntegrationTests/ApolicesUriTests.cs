using System.Net;
using System.Net.Http.Json;
using Seguros.Domain.Sinistros.Infrastructure.ApolicesApi;
using RegistrarSinistroRequest = Seguros.Domain.Sinistros.Features.RegistrarSinistro.Request;

namespace Seguros.IntegrationTests;

/// <summary>
/// Garante que o endereço da API de Apólices vem da configuração do ambiente
/// (ServicesSettings:ApolicesUri) e não de um valor fixo no código, inclusive quando o endereço do
/// ambiente inclui um caminho (cenário típico de QAS/PRD atrás de um gateway).
/// </summary>
public class ApolicesUriTests
{
    [Theory]
    [InlineData("http://localhost:5200", "http://localhost:5200/apolices/")]
    [InlineData("https://apolices.qas.seguros.example/", "https://apolices.qas.seguros.example/apolices/")]
    [InlineData("https://gateway.qas.seguros.example/apolices-api", "https://gateway.qas.seguros.example/apolices-api/apolices/")]
    public async Task DeveMontarAUrlDaConsultaAPartirDoBaseUrlDoAmbiente(string baseUrl, string prefixoEsperado)
    {
        Uri? urlChamada = null;

        await using var factory = new SegurosApiFactory(
            (request, _) =>
            {
                urlChamada = request.RequestUri;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            },
            sobrescreverEnderecos: s => s.ApolicesUri = baseUrl);

        var apoliceId = Guid.NewGuid();
        await factory.CreateClient().PostAsJsonAsync("/sinistros", new RegistrarSinistroRequest
        {
            ApoliceId = apoliceId,
            DataOcorrencia = new DateOnly(2026, 6, 1),
            TipoCobertura = "Colisao",
            Descricao = "Colisão na Av. Paulista"
        });

        Assert.Equal($"{prefixoEsperado}{apoliceId}", urlChamada?.ToString());
    }
}
