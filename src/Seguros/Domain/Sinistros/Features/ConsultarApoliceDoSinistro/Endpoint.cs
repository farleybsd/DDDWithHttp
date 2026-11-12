using FastEndpoints;
using Seguros.Domain.Sinistros.Contracts;
using Seguros.Common.Erros;

namespace Seguros.Domain.Sinistros.Features.ConsultarApoliceDoSinistro;

public sealed class Endpoint(Handler handler) : EndpointWithoutRequest<ApoliceDoSinistroResponse>
{
    public override void Configure()
    {
        Get("/sinistros/{id}/apolice");
        AllowAnonymous();
        Tags("Sinistros");
    }

    public override async Task<FastEndpoints.Void> HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var resultado = await handler.HandleAsync(id, ct);

        return resultado.IsFailure
            ? await Send.ResultAsync(resultado.Error!.ParaProblema())
            : await Send.OkAsync(resultado.Value, ct);
    }
}
