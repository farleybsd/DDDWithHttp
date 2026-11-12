using Seguros.Domain.Apolices.Contracts;
using Seguros.Domain.Apolices.Aggregates.Apolices;

namespace Seguros.Domain.Apolices.Features.ListarApolices;

public sealed class Handler(IApoliceRepository repository)
{
    public async Task<IReadOnlyList<ApoliceResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var apolices = await repository.ObterTodasAsync(cancellationToken);
        return apolices.Select(ConsultarApolice.Handler.Mapear).ToList();
    }
}
