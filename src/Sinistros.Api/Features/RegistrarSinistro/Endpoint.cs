using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Sinistros.Api.Contracts;

namespace Sinistros.Api.Features.RegistrarSinistro;

public sealed class Endpoint(Handler handler) : Endpoint<Request, SinistroResponse>
{
    public override void Configure()
    {
        Post("/sinistros");
        AllowAnonymous();
    }

    public override async Task<FastEndpoints.Void> HandleAsync(Request req, CancellationToken ct)
    {
        var resultado = await handler.HandleAsync(req, ct);

        return resultado switch
        {
            HandlerResultado.Sucesso sucesso => await Send.ResponseAsync(sucesso.Response, 201, ct),
            HandlerResultado.ApoliceNaoEncontrada => await Send.NotFoundAsync(ct),
            HandlerResultado.ApoliceInvalida invalida =>
                await Send.ResultAsync(Results.Json(new ErroResponse(invalida.Motivo), statusCode: 422)),
            HandlerResultado.ServicoIndisponivel indisponivel =>
                await Send.ResultAsync(Results.Json(new ErroResponse(indisponivel.Motivo), statusCode: 503)),
            _ => throw new InvalidOperationException("Resultado de handler não tratado.")
        };
    }
}
