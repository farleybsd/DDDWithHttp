using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Seguros.Common.Settings;
using Seguros.Domain.Sinistros.Aggregates.Sinistros;
using Seguros.Domain.Sinistros.Infrastructure.ApolicesApi;

namespace Seguros.IntegrationTests;

/// <summary>
/// Sobe a aplicação inteira (os dois contextos) em memória. Uma instância nova é criada por teste
/// (não é um fixture compartilhado) para que o estado do circuit breaker de um teste nunca vaze
/// para o próximo.
/// </summary>
/// <remarks>
/// Dois modos para a chamada HTTP Sinistros → Apólices:
/// <list type="bullet">
///   <item><paramref name="responder"/> informado: o handler primário do cliente HTTP é trocado por
///   um <see cref="ScriptedHttpMessageHandler"/> que simula o contexto de Apólices (sucesso, 404,
///   falhas, lentidão). O pipeline de resiliência real continua ativo por cima dele.</item>
///   <item><paramref name="responder"/> nulo: a requisição HTTP é entregue ao próprio
///   <see cref="TestServer"/>, ou seja, chega de verdade aos endpoints do contexto de Apólices
///   desta mesma aplicação — exatamente o que acontece em produção, só que sem socket.</item>
/// </list>
/// </remarks>
public sealed class SegurosApiFactory(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? responder = null,
    Action<HttpResilienceSettings>? sobrescreverResiliencia = null,
    Action<ServicesSettings>? sobrescreverEnderecos = null,
    Action<IServiceCollection>? sobrescreverServicos = null,
    string ambiente = "Development") : WebApplicationFactory<Program>
{
    public ScriptedHttpMessageHandler Handler { get; } = new(responder ?? ((_, _) => throw new InvalidOperationException(
        "Factory criada sem responder: a chamada vai para o contexto de Apólices real.")));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(ambiente);

        builder.ConfigureTestServices(services =>
        {
            var cliente = services.AddHttpClient<IApolicesApiClient, ApolicesApiClient>();

            if (responder is not null)
                cliente.ConfigurePrimaryHttpMessageHandler(() => Handler);
            else
                cliente.ConfigurePrimaryHttpMessageHandler(sp => new HandlerDoProprioServidor(sp));

            if (sobrescreverResiliencia is not null)
                services.PostConfigure(sobrescreverResiliencia);

            if (sobrescreverEnderecos is not null)
                services.PostConfigure(sobrescreverEnderecos);

            // Roda depois dos módulos do Program: o último registro vence.
            sobrescreverServicos?.Invoke(services);
        });
    }

    /// <summary>
    /// Entrega a requisição ao próprio <see cref="TestServer"/>. O handler real só é obtido na
    /// primeira requisição, porque o FastEndpoints instancia os endpoints (e, com eles, o cliente
    /// HTTP) durante a subida, antes de o servidor estar pronto.
    /// </summary>
    private sealed class HandlerDoProprioServidor(IServiceProvider sp) : HttpMessageHandler
    {
        private readonly Lazy<HttpMessageInvoker> _invoker = new(() =>
            new HttpMessageInvoker(((TestServer)sp.GetRequiredService<IServer>()).CreateHandler()));

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _invoker.Value.SendAsync(request, cancellationToken);
    }
}
