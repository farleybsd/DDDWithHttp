using System.Collections.Concurrent;
using Sinistros.Api.Domain;

namespace Sinistros.Api.Infrastructure;

public sealed class InMemorySinistroRepository : ISinistroRepository
{
    private readonly ConcurrentDictionary<Guid, Sinistro> _sinistros = new();

    public Task AdicionarAsync(Sinistro sinistro, CancellationToken cancellationToken)
    {
        _sinistros[sinistro.Id] = sinistro;
        return Task.CompletedTask;
    }

    public Task<Sinistro?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _sinistros.TryGetValue(id, out var sinistro);
        return Task.FromResult(sinistro);
    }

    public Task<IReadOnlyList<Sinistro>> ListarPorApoliceAsync(Guid apoliceId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Sinistro> sinistros = _sinistros.Values
            .Where(s => s.ApoliceId == apoliceId)
            .OrderByDescending(s => s.DataRegistro)
            .ToList();

        return Task.FromResult(sinistros);
    }
}
