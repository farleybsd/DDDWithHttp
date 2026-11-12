using Seguros.Common;
using Seguros.Domain.Sinistros.Contracts;
using Seguros.Domain.Sinistros.Aggregates.Sinistros;

namespace Seguros.Domain.Sinistros.Features.ConsultarSinistro;

public sealed class Handler(ISinistroRepository repository)
{
    public async Task<Result<SinistroResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var sinistro = await repository.ObterPorIdAsync(id, cancellationToken);
        if (sinistro is null)
            return ErrosDeSinistro.SinistroNaoEncontrado(id);

        return Result.Success(RegistrarSinistro.Handler.Mapear(sinistro));
    }
}
