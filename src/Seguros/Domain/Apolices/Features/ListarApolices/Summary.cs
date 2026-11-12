using Seguros.Domain.Apolices.Contracts;
using FastEndpoints;

namespace Seguros.Domain.Apolices.Features.ListarApolices;

public sealed class Summary : Summary<Endpoint>
{
    public Summary()
    {
        Summary = "Lista todas as apólices cadastradas";
        Description = "Retorna a coleção completa de apólices de seguro cadastradas, " +
            "sem paginação.";
        Response<IReadOnlyList<ApoliceResponse>>(200, "Coleção de apólices");
    }
}
