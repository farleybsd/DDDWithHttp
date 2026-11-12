namespace Seguros.Domain.Sinistros.Aggregates.Sinistros;

public interface ISinistroRepository
{
    Task AdicionarAsync(Sinistro sinistro, CancellationToken cancellationToken);
    Task<Sinistro?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Sinistro>> ListarPorApoliceAsync(Guid apoliceId, CancellationToken cancellationToken);
}
