using FastEndpoints;
using Sinistros.Api.Contracts;

namespace Sinistros.Api.Features.ConsultarSinistro;

public sealed class Endpoint(Handler handler) : EndpointWithoutRequest<SinistroResponse>
{
    public override void Configure()
    {
        Get("/sinistros/{id}");
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
