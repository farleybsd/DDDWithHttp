using System.Net;
using System.Net.Http.Json;
using Seguros.Domain.Sinistros.Contracts;
using RegistrarSinistroRequest = Seguros.Domain.Sinistros.Features.RegistrarSinistro.Request;

namespace Seguros.IntegrationTests;

/// <summary>
/// Sem nenhum stub: o contexto de Sinistros faz a requisição HTTP <c>GET /apolices/{id}</c> e ela
/// é atendida pelos endpoints reais do contexto de Apólices desta mesma aplicação, com os dados
/// fictícios do repositório em memória.
/// </summary>
public class ComunicacaoEntreContextosTests
{
    private static readonly Guid ApoliceVigente = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ApoliceExpirada = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ApoliceSemRoubo = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task RegistrarSinistro_ConsultaOContextoDeApolicesPorHttp()
    {
        await using var factory = new SegurosApiFactory();
        var client = factory.CreateClient();

        var registro = await client.PostAsJsonAsync("/sinistros", Request(ApoliceVigente, new DateOnly(2026, 6, 1), "Colisao"));
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);
        var sinistro = await registro.Content.ReadFromJsonAsync<SinistroResponse>();

        var resposta = await client.GetAsync($"/sinistros/{sinistro!.Id}/apolice");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var apolice = await resposta.Content.ReadFromJsonAsync<ApoliceDoSinistroResponse>();
        Assert.Equal(ApoliceVigente, apolice!.ApoliceId);
        Assert.Equal(new DateOnly(2026, 1, 1), apolice.VigenciaInicio);
        Assert.Equal(new DateOnly(2026, 12, 31), apolice.VigenciaFim);
        Assert.Equal(["Colisao", "Roubo", "Incendio"], apolice.Coberturas);
        Assert.True(apolice.VigenteNaDataOcorrencia);
        Assert.True(apolice.PossuiCoberturaDoSinistro);
    }

    [Fact]
    public async Task RegistrarSinistro_Retorna422_QuandoApoliceDoOutroContextoEstaExpirada()
    {
        await using var factory = new SegurosApiFactory();

        var resposta = await factory.CreateClient().PostAsJsonAsync("/sinistros", Request(ApoliceExpirada, new DateOnly(2026, 6, 1), "Colisao"));

        Assert.Equal((HttpStatusCode)422, resposta.StatusCode);
        await ProblemaAssert.EhProblemaAsync(resposta, "apolice-fora-de-vigencia");
    }

    [Fact]
    public async Task RegistrarSinistro_Retorna422_QuandoApoliceDoOutroContextoNaoTemACobertura()
    {
        await using var factory = new SegurosApiFactory();

        var resposta = await factory.CreateClient().PostAsJsonAsync("/sinistros", Request(ApoliceSemRoubo, new DateOnly(2026, 6, 15), "Roubo"));

        Assert.Equal((HttpStatusCode)422, resposta.StatusCode);
        await ProblemaAssert.EhProblemaAsync(resposta, "cobertura-nao-contratada");
    }

    [Fact]
    public async Task RegistrarSinistro_Retorna404_QuandoOContextoDeApolicesResponde404()
    {
        await using var factory = new SegurosApiFactory();

        var resposta = await factory.CreateClient().PostAsJsonAsync("/sinistros", Request(Guid.NewGuid(), new DateOnly(2026, 6, 1), "Colisao"));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        await ProblemaAssert.EhProblemaAsync(resposta, "apolice-nao-encontrada");
    }

    private static RegistrarSinistroRequest Request(Guid apoliceId, DateOnly data, string cobertura) => new()
    {
        ApoliceId = apoliceId,
        DataOcorrencia = data,
        TipoCobertura = cobertura,
        Descricao = "Sinistro de teste"
    };
}
