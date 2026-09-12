using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Sinistros.Api.Contracts;

namespace Sinistros.Api.Features.ConsultarApoliceDoSinistro;

public sealed class Endpoint(Handler handler) : EndpointWithoutRequest<ApoliceDoSinistroResponse>
{
    public override void Configure()
    {
        Get("/sinistros/{id}/apolice");
        AllowAnonymous();
    }

    public override async Task<FastEndpoints.Void> HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var resultado = await handler.HandleAsync(id, ct);

        return resultado switch
        {
            HandlerResultado.Sucesso sucesso => await Send.OkAsync(sucesso.Response, ct),
            HandlerResultado.SinistroNaoEncontrado =>
                await Send.ResultAsync(Results.Json(new ErroResponse("Sinistro não encontrado."), statusCode: 404)),
            HandlerResultado.ApoliceNaoEncontrada =>
                await Send.ResultAsync(Results.Json(new ErroResponse("A apólice vinculada a este sinistro não foi encontrada na API de Apólices."), statusCode: 404)),
            HandlerResultado.ServicoIndisponivel indisponivel =>
                await Send.ResultAsync(Results.Json(new ErroResponse(indisponivel.Motivo), statusCode: 503)),
            _ => throw new InvalidOperationException("Resultado de handler não tratado.")
        };
    }
}
