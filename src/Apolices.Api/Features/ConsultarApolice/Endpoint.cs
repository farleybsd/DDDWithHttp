using Apolices.Api.Contracts;
using FastEndpoints;

namespace Apolices.Api.Features.ConsultarApolice;

public sealed class Endpoint(Handler handler) : EndpointWithoutRequest<ApoliceResponse>
{
    public override void Configure()
    {
        Get("/apolices/{id}");
        AllowAnonymous();
    }

    public override async Task<FastEndpoints.Void> HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var resultado = await handler.HandleAsync(id, ct);

        return resultado is null
            ? await Send.NotFoundAsync(ct)
            : await Send.OkAsync(resultado, ct);
    }
}
