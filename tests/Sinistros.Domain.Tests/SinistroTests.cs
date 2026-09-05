using Sinistros.Api.Domain;

namespace Sinistros.Domain.Tests;

public class SinistroTests
{
    private static readonly Guid ApoliceId = Guid.NewGuid();

    private static ApoliceReferenciada CriarApoliceVigenteComCobertura(params TipoCobertura[] coberturas) => new(
        ApoliceId,
        new DateOnly(2026, 1, 1),
        new DateOnly(2026, 12, 31),
        coberturas);

    [Fact]
    public void Registrar_DeveTerSucesso_QuandoApoliceVigenteEComCobertura()
    {
        var apolice = CriarApoliceVigenteComCobertura(TipoCobertura.Colisao);

        var resultado = Sinistro.Registrar(
            ApoliceId,
            new DateOnly(2026, 6, 1),
            TipoCobertura.Colisao,
            "Colisão na Av. Paulista",
            apolice);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(StatusSinistro.Registrado, resultado.Value.Status);
    }

    [Fact]
    public void Registrar_DeveFalhar_QuandoApoliceForaDeVigenciaNaDataDaOcorrencia()
    {
        var apolice = CriarApoliceVigenteComCobertura(TipoCobertura.Colisao);

        var resultado = Sinistro.Registrar(
            ApoliceId,
            new DateOnly(2025, 6, 1),
            TipoCobertura.Colisao,
            "Colisão fora do período de vigência",
            apolice);

        Assert.True(resultado.IsFailure);
        Assert.Contains("vigente", resultado.Error);
    }

    [Fact]
    public void Registrar_DeveFalhar_QuandoApoliceNaoPossuiACoberturaSolicitada()
    {
        var apolice = CriarApoliceVigenteComCobertura(TipoCobertura.Incendio);

        var resultado = Sinistro.Registrar(
            ApoliceId,
            new DateOnly(2026, 6, 1),
            TipoCobertura.Roubo,
            "Furto do veículo",
            apolice);

        Assert.True(resultado.IsFailure);
        Assert.Contains("cobertura", resultado.Error);
    }

    [Fact]
    public void Registrar_DeveFalhar_QuandoDescricaoVazia()
    {
        var apolice = CriarApoliceVigenteComCobertura(TipoCobertura.Colisao);

        var resultado = Sinistro.Registrar(ApoliceId, new DateOnly(2026, 6, 1), TipoCobertura.Colisao, "", apolice);

        Assert.True(resultado.IsFailure);
    }
}
