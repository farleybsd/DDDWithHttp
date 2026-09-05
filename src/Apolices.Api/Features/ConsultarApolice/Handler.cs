using Apolices.Api.Contracts;
using Apolices.Api.Domain;

namespace Apolices.Api.Features.ConsultarApolice;

public sealed class Handler(IApoliceRepository repository)
{
    public async Task<ApoliceResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var apolice = await repository.ObterPorIdAsync(id, cancellationToken);
        return apolice is null ? null : Mapear(apolice);
    }

    internal static ApoliceResponse Mapear(Apolice apolice) => new(
        apolice.Id,
        apolice.Numero,
        new SeguradoResponse(apolice.Segurado.Nome, apolice.Segurado.Documento),
        new VigenciaResponse(apolice.Vigencia.Inicio, apolice.Vigencia.Fim),
        apolice.Coberturas.Select(c => new CoberturaResponse(c.Tipo.ToString(), c.LimiteIndenizacao)).ToList());
}
