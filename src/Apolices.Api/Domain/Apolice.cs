using Apolices.Api.Common;

namespace Apolices.Api.Domain;

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
            return Result.Failure<Apolice>("O número da apólice é obrigatório.");

        var listaCoberturas = coberturas.ToList();
        if (listaCoberturas.Count == 0)
            return Result.Failure<Apolice>("A apólice deve ter ao menos uma cobertura.");

        return Result.Success(new Apolice(id, numero, segurado, vigencia, listaCoberturas));
    }

    public bool EstaVigenteEm(DateOnly data) => Vigencia.EstaVigenteEm(data);

    public bool PossuiCobertura(TipoCobertura tipo) => _coberturas.Any(c => c.Tipo == tipo);
}
