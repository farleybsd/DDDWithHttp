using Sinistros.Api.Contracts;
using Sinistros.Api.Domain;

namespace Sinistros.Api.Features.RegistrarSinistro;

/// <summary>
/// Resultado do caso de uso, distinguindo os motivos de falha para que o Endpoint escolha o
/// código HTTP correto (404, 422 ou 503).
/// </summary>
public abstract record HandlerResultado
{
    public sealed record Sucesso(SinistroResponse Response) : HandlerResultado;

    public sealed record ApoliceNaoEncontrada : HandlerResultado;

    public sealed record ApoliceInvalida(string Motivo) : HandlerResultado;

    public sealed record ServicoIndisponivel(string Motivo) : HandlerResultado;
}

public sealed class Handler(
    IApolicesApiClient apolicesApiClient,
    ISinistroRepository repository,
    ILogger<Handler> logger)
{
    public async Task<HandlerResultado> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var consulta = await apolicesApiClient.ConsultarApoliceAsync(request.ApoliceId, cancellationToken);

        return consulta switch
        {
            ApoliceConsultaResultado.NaoEncontrada => new HandlerResultado.ApoliceNaoEncontrada(),

            ApoliceConsultaResultado.Indisponivel indisponivel => LogEIndisponivel(request.ApoliceId, indisponivel),

            ApoliceConsultaResultado.Encontrada encontrada => await RegistrarAsync(request, encontrada.Apolice, cancellationToken),

            _ => throw new InvalidOperationException("Resultado de consulta de apólice não tratado.")
        };
    }

    private HandlerResultado LogEIndisponivel(Guid apoliceId, ApoliceConsultaResultado.Indisponivel indisponivel)
    {
        logger.LogWarning(
            "Sinistro não registrado: API de Apólices indisponível para a apólice {ApoliceId}. Motivo: {Motivo}",
            apoliceId,
            indisponivel.Motivo);

        return new HandlerResultado.ServicoIndisponivel(indisponivel.Motivo);
    }

    private async Task<HandlerResultado> RegistrarAsync(Request request, ApoliceReferenciada apolice, CancellationToken cancellationToken)
    {
        var tipoCobertura = Enum.Parse<TipoCobertura>(request.TipoCobertura, ignoreCase: true);

        var registro = Sinistro.Registrar(request.ApoliceId, request.DataOcorrencia, tipoCobertura, request.Descricao, apolice);
        if (registro.IsFailure)
            return new HandlerResultado.ApoliceInvalida(registro.Error!);

        var sinistro = registro.Value;
        await repository.AdicionarAsync(sinistro, cancellationToken);

        return new HandlerResultado.Sucesso(Mapear(sinistro));
    }

    internal static SinistroResponse Mapear(Sinistro sinistro) => new(
        sinistro.Id,
        sinistro.ApoliceId,
        sinistro.DataOcorrencia,
        sinistro.TipoCobertura.ToString(),
        sinistro.Descricao,
        sinistro.Status.ToString(),
        sinistro.DataRegistro);
}
