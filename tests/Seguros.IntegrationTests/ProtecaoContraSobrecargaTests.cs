using System.Net;
using System.Net.Http.Json;
using Seguros.Domain.Sinistros.Infrastructure.ApolicesApi;
using RegistrarSinistroRequest = Seguros.Domain.Sinistros.Features.RegistrarSinistro.Request;

namespace Seguros.IntegrationTests;

/// <summary>
/// Proteções das chamadas internas (mesmo processo) contra a aplicação sobrecarregar a si mesma:
/// retry reduzido ou desligado e limite de chamadas simultâneas.
/// </summary>
public class ProtecaoContraSobrecargaTests
{
    private static readonly Guid ApoliceId = Guid.NewGuid();

    [Fact]
    public async Task ComRetryPadrao_FazNoMaximoUmaRetentativa()
    {
        // Sem sobrescrever o retry: vale o appsettings.json (MaxRetryAttempts = 1).
        await using var factory = new SegurosApiFactory(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            sobrescreverResiliencia: o =>
            {
                o.Retry.BaseDelay = TimeSpan.FromMilliseconds(10);
                o.CircuitBreaker.MinimumThroughput = 1000;
            });

        var resposta = await factory.CreateClient().PostAsJsonAsync("/sinistros", RequestValido());

        Assert.Equal((HttpStatusCode)503, resposta.StatusCode);
        Assert.Equal(2, factory.Handler.NumeroDeChamadas); // 1 tentativa + 1 retry
    }

    [Fact]
    public async Task ComRetryZero_NaoRetenta()
    {
        await using var factory = new SegurosApiFactory(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            sobrescreverResiliencia: o =>
            {
                o.Retry.MaxRetryAttempts = 0;
                o.CircuitBreaker.MinimumThroughput = 1000;
            });

        var resposta = await factory.CreateClient().PostAsJsonAsync("/sinistros", RequestValido());

        Assert.Equal((HttpStatusCode)503, resposta.StatusCode);
        Assert.Equal(1, factory.Handler.NumeroDeChamadas);
    }

    [Fact]
    public async Task AcimaDoLimiteDeConcorrencia_RecusaNaHoraCom503()
    {
        var liberar = new TaskCompletionSource();
        var primeiraChegou = new TaskCompletionSource();

        await using var factory = new SegurosApiFactory(
            async (_, ct) =>
            {
                primeiraChegou.TrySetResult();
                await liberar.Task.WaitAsync(ct); // segura a 1ª chamada ocupando o único "lugar"
                return JsonResponse(HttpStatusCode.OK, ApoliceVigenteComColisao());
            },
            sobrescreverResiliencia: o =>
            {
                o.ConcurrencyLimiter.PermitLimit = 1;
                o.ConcurrencyLimiter.QueueLimit = 0;
                o.Timeout = TimeSpan.FromSeconds(10);
                o.CircuitBreaker.MinimumThroughput = 1000;
            });

        var client = factory.CreateClient();

        var primeira = client.PostAsJsonAsync("/sinistros", RequestValido());
        await primeiraChegou.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var segunda = await client.PostAsJsonAsync("/sinistros", RequestValido());

        Assert.Equal((HttpStatusCode)503, segunda.StatusCode);
        await ProblemaAssert.EhProblemaAsync(segunda, "apolices-api-indisponivel");
        Assert.Equal(1, factory.Handler.NumeroDeChamadas); // a 2ª nem chegou a sair (nem retentou)

        liberar.SetResult();
        Assert.Equal(HttpStatusCode.Created, (await primeira).StatusCode);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, object corpo) => new(statusCode)
    {
        Content = JsonContent.Create(corpo)
    };

    private static ApoliceApiDto ApoliceVigenteComColisao() => new(
        ApoliceId,
        "AP-0001",
        new SeguradoApiDto("Ana Souza", "111.111.111-11"),
        new VigenciaApiDto(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
        [new CoberturaApiDto("Colisao", 50_000m)]);

    private static RegistrarSinistroRequest RequestValido() => new()
    {
        ApoliceId = ApoliceId,
        DataOcorrencia = new DateOnly(2026, 6, 1),
        TipoCobertura = "Colisao",
        Descricao = "Colisão na Av. Paulista"
    };
}
