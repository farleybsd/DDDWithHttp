namespace Apolices.Api.Domain;

public interface IApoliceRepository
{
    Task<Apolice?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Apolice>> ObterTodasAsync(CancellationToken cancellationToken);
}
