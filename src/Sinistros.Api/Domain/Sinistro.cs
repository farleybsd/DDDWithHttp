using Sinistros.Api.Common;

namespace Sinistros.Api.Domain;

public sealed class Sinistro
{
    public Guid Id { get; }
    public Guid ApoliceId { get; }
    public DateOnly DataOcorrencia { get; }
    public TipoCobertura TipoCobertura { get; }
    public string Descricao { get; }
    public StatusSinistro Status { get; }
    public DateTime DataRegistro { get; }

    private Sinistro(
        Guid id,
        Guid apoliceId,
        DateOnly dataOcorrencia,
        TipoCobertura tipoCobertura,
        string descricao,
        StatusSinistro status,
        DateTime dataRegistro)
    {
        Id = id;
        ApoliceId = apoliceId;
        DataOcorrencia = dataOcorrencia;
        TipoCobertura = tipoCobertura;
        Descricao = descricao;
        Status = status;
        DataRegistro = dataRegistro;
    }

    /// <summary>
    /// Registra um sinistro validando, a partir dos dados já consultados da API de Apólices
    /// (traduzidos para <see cref="ApoliceReferenciada"/>), que a apólice estava vigente na data
    /// da ocorrência e que possui a cobertura solicitada.
    /// </summary>
    public static Result<Sinistro> Registrar(
        Guid apoliceId,
        DateOnly dataOcorrencia,
        TipoCobertura tipoCobertura,
        string descricao,
        ApoliceReferenciada apolice)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            return ErrosDeSinistro.DescricaoObrigatoria;

        if (!apolice.EstaVigenteEm(dataOcorrencia))
            return ErrosDeSinistro.ApoliceForaDeVigencia(dataOcorrencia, apolice.VigenciaInicio, apolice.VigenciaFim);

        if (!apolice.PossuiCobertura(tipoCobertura))
            return ErrosDeSinistro.CoberturaNaoContratada(tipoCobertura);

        var sinistro = new Sinistro(
            Guid.NewGuid(),
            apoliceId,
            dataOcorrencia,
            tipoCobertura,
            descricao,
            StatusSinistro.Registrado,
            DateTime.UtcNow);

        return Result.Success(sinistro);
    }
}
