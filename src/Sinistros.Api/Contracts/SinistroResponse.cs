namespace Sinistros.Api.Contracts;

public sealed record SinistroResponse(
    Guid Id,
    Guid ApoliceId,
    DateOnly DataOcorrencia,
    string TipoCobertura,
    string Descricao,
    string Status,
    DateTime DataRegistro);
