using FastEndpoints;
using Seguros.Domain.Sinistros.Contracts;
using Seguros.Common.Erros;

namespace Seguros.Domain.Sinistros.Features.RegistrarSinistro;

public sealed class Endpoint(Handler handler) : Endpoint<Request, SinistroResponse>
{
    public override void Configure()
    {
        Post("/sinistros");
        AllowAnonymous();
        Tags("Sinistros");
    }

    public override async Task<FastEndpoints.Void> HandleAsync(Request req, CancellationToken ct)
    {
        var resultado = await handler.HandleAsync(req, ct);

        return resultado.IsFailure
            ? await Send.ResultAsync(resultado.Error!.ParaProblema())
            : await Send.ResponseAsync(resultado.Value, 201, ct);
    }
}
