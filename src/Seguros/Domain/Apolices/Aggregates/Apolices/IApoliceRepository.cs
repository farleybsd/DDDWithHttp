namespace Seguros.Domain.Apolices.Aggregates.Apolices;

public interface IApoliceRepository
{
    Task<Apolice?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Apolice>> ObterTodasAsync(CancellationToken cancellationToken);
}
