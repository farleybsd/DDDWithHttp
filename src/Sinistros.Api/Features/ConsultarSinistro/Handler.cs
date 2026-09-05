using Sinistros.Api.Contracts;
using Sinistros.Api.Domain;

namespace Sinistros.Api.Features.ConsultarSinistro;

public sealed class Handler(ISinistroRepository repository)
{
    public async Task<SinistroResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var sinistro = await repository.ObterPorIdAsync(id, cancellationToken);
        return sinistro is null ? null : RegistrarSinistro.Handler.Mapear(sinistro);
    }
}
