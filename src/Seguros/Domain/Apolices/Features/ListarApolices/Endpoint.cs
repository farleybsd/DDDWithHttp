using Seguros.Domain.Apolices.Contracts;
using FastEndpoints;

namespace Seguros.Domain.Apolices.Features.ListarApolices;

public sealed class Endpoint(Handler handler) : EndpointWithoutRequest<IReadOnlyList<ApoliceResponse>>
{
    public override void Configure()
    {
        Get("/apolices");
        AllowAnonymous();
        Tags("Apolices");
    }

    public override async Task<FastEndpoints.Void> HandleAsync(CancellationToken ct)
    {
        var resultado = await handler.HandleAsync(ct);
        return await Send.OkAsync(resultado, ct);
    }
}
