using Seguros.Common;

namespace Seguros.Domain.Apolices.Aggregates.Apolices;

public sealed class Apolice
{
    private readonly List<Cobertura> _coberturas;

    public Guid Id { get; }
    public string Numero { get; }
    public Segurado Segurado { get; }
    public PeriodoVigencia Vigencia { get; }
    public IReadOnlyList<Cobertura> Coberturas => _coberturas;

    private Apolice(Guid id, string numero, Segurado segurado, PeriodoVigencia vigencia, List<Cobertura> coberturas)
    {
        Id = id;
        Numero = numero;
        Segurado = segurado;
        Vigencia = vigencia;
        _coberturas = coberturas;
    }

    public static Result<Apolice> Criar(
        Guid id,
        string numero,
        Segurado segurado,
        PeriodoVigencia vigencia,
        IEnumerable<Cobertura> coberturas)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return ErrosDeApolice.NumeroObrigatorio;

        var listaCoberturas = coberturas.ToList();
        if (listaCoberturas.Count == 0)
            return ErrosDeApolice.SemCobertura;

        return Result.Success(new Apolice(id, numero, segurado, vigencia, listaCoberturas));
    }

    public bool EstaVigenteEm(DateOnly data) => Vigencia.EstaVigenteEm(data);

    public bool PossuiCobertura(TipoCobertura tipo) => _coberturas.Any(c => c.Tipo == tipo);
}
