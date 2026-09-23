using FastEndpoints;
using Sinistros.Api.Contracts;

namespace Sinistros.Api.Features.ConsultarSinistro;

public sealed class Summary : Summary<Endpoint>
{
    public Summary()
    {
        Summary = "Consulta um sinistro pelo identificador";
        Description = "Retorna os dados de um sinistro previamente registrado.";
        Params["id"] = "Identificador do sinistro";
        Response<SinistroResponse>(200, "Sinistro encontrado");
        Response(404, "Nenhum sinistro foi encontrado com o Id informado");
    }
}
