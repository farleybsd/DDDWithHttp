namespace Apolices.Api.Contracts;

public sealed record ApoliceResponse(
    Guid Id,
    string Numero,
    SeguradoResponse Segurado,
    VigenciaResponse Vigencia,
    IReadOnlyList<CoberturaResponse> Coberturas);

public sealed record SeguradoResponse(string Nome, string Documento);

public sealed record VigenciaResponse(DateOnly Inicio, DateOnly Fim);

public sealed record CoberturaResponse(string Tipo, decimal LimiteIndenizacao);
