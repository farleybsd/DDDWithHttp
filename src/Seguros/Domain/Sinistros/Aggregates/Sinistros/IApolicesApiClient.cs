namespace Seguros.Domain.Sinistros.Aggregates.Sinistros;

/// <summary>
/// Porta (definida pelo domínio de Sinistros) para consultar a API de Apólices. A implementação
/// concreta, que efetivamente fala HTTP, vive na camada de infraestrutura.
/// </summary>
public interface IApolicesApiClient
{
    Task<ApoliceConsultaResultado> ConsultarApoliceAsync(Guid apoliceId, CancellationToken cancellationToken);
}
