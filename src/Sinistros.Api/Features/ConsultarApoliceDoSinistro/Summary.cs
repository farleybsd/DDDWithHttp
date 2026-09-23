using FastEndpoints;
using Sinistros.Api.Contracts;

namespace Sinistros.Api.Features.ConsultarApoliceDoSinistro;

public sealed class Summary : Summary<Endpoint>
{
    public Summary()
    {
        Summary = "Consulta a apólice vinculada a um sinistro";
        Description = "Busca em tempo real, na API de Apólices, os dados da apólice " +
            "associada ao sinistro e indica se ela estava vigente e possuía a cobertura " +
            "na data da ocorrência.";
        Params["id"] = "Identificador do sinistro";
        Response<ApoliceDoSinistroResponse>(200, "Dados da apólice vinculada ao sinistro");
        Response<ErroResponse>(404, "Sinistro não encontrado, ou a apólice vinculada não foi encontrada na API de Apólices");
        Response<ErroResponse>(503, "A API de Apólices está indisponível no momento");
    }
}
