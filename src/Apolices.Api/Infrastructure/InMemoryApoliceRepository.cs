using System.Collections.Concurrent;
using Apolices.Api.Domain;

namespace Apolices.Api.Infrastructure;

/// <summary>
/// Repositório em memória com dados fictícios fixos, pensados para exercitar os cenários de
/// validação usados pela API de Sinistros (vigente com cobertura, expirada, vigente sem cobertura).
/// </summary>
public sealed class InMemoryApoliceRepository : IApoliceRepository
{
    private readonly ConcurrentDictionary<Guid, Apolice> _apolices = new();

    public InMemoryApoliceRepository()
    {
        foreach (var apolice in CriarDadosFicticios())
            _apolices[apolice.Id] = apolice;
    }

    public Task<Apolice?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _apolices.TryGetValue(id, out var apolice);
        return Task.FromResult(apolice);
    }

    public Task<IReadOnlyList<Apolice>> ObterTodasAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Apolice> todas = _apolices.Values.OrderBy(a => a.Numero).ToList();
        return Task.FromResult(todas);
    }

    private static IEnumerable<Apolice> CriarDadosFicticios()
    {
        yield return Apolice.Criar(
            id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            numero: "AP-0001",
            segurado: new Segurado("Ana Souza", "111.111.111-11"),
            vigencia: new PeriodoVigencia(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
            coberturas:
            [
                new Cobertura(TipoCobertura.Colisao, 50_000m),
                new Cobertura(TipoCobertura.Roubo, 30_000m),
                new Cobertura(TipoCobertura.Incendio, 40_000m)
            ]).Value;

        yield return Apolice.Criar(
            id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            numero: "AP-0002",
            segurado: new Segurado("Bruno Lima", "222.222.222-22"),
            vigencia: new PeriodoVigencia(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)),
            coberturas:
            [
                new Cobertura(TipoCobertura.Colisao, 20_000m)
            ]).Value;

        yield return Apolice.Criar(
            id: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            numero: "AP-0003",
            segurado: new Segurado("Carla Dias", "333.333.333-33"),
            vigencia: new PeriodoVigencia(new DateOnly(2026, 6, 1), new DateOnly(2027, 5, 31)),
            coberturas:
            [
                new Cobertura(TipoCobertura.Incendio, 15_000m),
                new Cobertura(TipoCobertura.DanosTerceiros, 10_000m)
            ]).Value;
    }
}
