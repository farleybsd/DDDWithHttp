using Sinistros.Api.Contracts;
using Sinistros.Api.Domain;

namespace Sinistros.Api.Features.ConsultarApoliceDoSinistro;

/// <summary>
/// Resultado do caso de uso, distinguindo os motivos de falha para que o Endpoint escolha o
/// código HTTP correto (404 ou 503).
/// </summary>
public abstract record HandlerResultado
{
    public sealed record Sucesso(ApoliceDoSinistroResponse Response) : HandlerResultado;

    public sealed record SinistroNaoEncontrado : HandlerResultado;

    public sealed record ApoliceNaoEncontrada : HandlerResultado;

    public sealed record ServicoIndisponivel(string Motivo) : HandlerResultado;
}

/// <summary>
/// Dado um sinistro já registrado, consulta a API de Apólices (<c>GET /apolices/{id}</c>) para
/// devolver a apólice vinculada a ele. A leitura é feita sempre ao vivo: este contexto guarda
/// apenas o identificador da apólice, nunca uma cópia dos dados do contexto de Apólices.
/// </summary>
public sealed class Handler(
    ISinistroRepository repository,
    IApolicesApiClient apolicesApiClient,
    ILogger<Handler> logger)
{
    public async Task<HandlerResultado> HandleAsync(Guid sinistroId, CancellationToken cancellationToken)
    {
        var sinistro = await repository.ObterPorIdAsync(sinistroId, cancellationToken);
        if (sinistro is null)
            return new HandlerResultado.SinistroNaoEncontrado();

        var consulta = await apolicesApiClient.ConsultarApoliceAsync(sinistro.ApoliceId, cancellationToken);

        return consulta switch
        {
            ApoliceConsultaResultado.Encontrada encontrada =>
                new HandlerResultado.Sucesso(Mapear(sinistro, encontrada.Apolice)),

            ApoliceConsultaResultado.NaoEncontrada => LogEApoliceNaoEncontrada(sinistro),

            ApoliceConsultaResultado.Indisponivel indisponivel => LogEIndisponivel(sinistro, indisponivel),

            _ => throw new InvalidOperationException("Resultado de consulta de apólice não tratado.")
        };
    }

    private HandlerResultado LogEApoliceNaoEncontrada(Sinistro sinistro)
    {
        logger.LogWarning(
            "O sinistro {SinistroId} referencia a apólice {ApoliceId}, que não existe mais na API de Apólices.",
            sinistro.Id,
            sinistro.ApoliceId);

        return new HandlerResultado.ApoliceNaoEncontrada();
    }

    private HandlerResultado LogEIndisponivel(Sinistro sinistro, ApoliceConsultaResultado.Indisponivel indisponivel)
    {
        logger.LogWarning(
            "Apólice do sinistro {SinistroId} não consultada: API de Apólices indisponível. Motivo: {Motivo}",
            sinistro.Id,
            indisponivel.Motivo);

        return new HandlerResultado.ServicoIndisponivel(indisponivel.Motivo);
    }

    private static ApoliceDoSinistroResponse Mapear(Sinistro sinistro, ApoliceReferenciada apolice) => new(
        sinistro.Id,
        apolice.ApoliceId,
        sinistro.DataOcorrencia,
        sinistro.TipoCobertura.ToString(),
        apolice.VigenciaInicio,
        apolice.VigenciaFim,
        apolice.Coberturas.Select(c => c.ToString()).ToList(),
        apolice.EstaVigenteEm(sinistro.DataOcorrencia),
        apolice.PossuiCobertura(sinistro.TipoCobertura));
}
