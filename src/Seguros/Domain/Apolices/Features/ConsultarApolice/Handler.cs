using Seguros.Common;
using Seguros.Domain.Apolices.Contracts;
using Seguros.Domain.Apolices.Aggregates.Apolices;

namespace Seguros.Domain.Apolices.Features.ConsultarApolice;

public sealed class Handler(IApoliceRepository repository)
{
    public async Task<Result<ApoliceResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var apolice = await repository.ObterPorIdAsync(id, cancellationToken);
        if (apolice is null)
            return ErrosDeApolice.NaoEncontrada(id);

        return Result.Success(Mapear(apolice));
    }

    internal static ApoliceResponse Mapear(Apolice apolice) => new(
        apolice.Id,
        apolice.Numero,
        new SeguradoResponse(apolice.Segurado.Nome, apolice.Segurado.Documento),
        new VigenciaResponse(apolice.Vigencia.Inicio, apolice.Vigencia.Fim),
        apolice.Coberturas.Select(c => new CoberturaResponse(c.Tipo.ToString(), c.LimiteIndenizacao)).ToList());
}
