using Apolices.Api.Common;

namespace Apolices.Api.Domain;

/// <summary>
/// Catálogo dos erros esperados do contexto de Apólices. Cada <see cref="Erro.Codigo"/> é
/// publicado para os consumidores (aparece no <c>type</c> e no <c>code</c> do ProblemDetails) e
/// não pode ser renomeado: mudou o significado, crie um código novo e mantenha o antigo.
/// </summary>
public static class ErrosDeApolice
{
    public static Erro NaoEncontrada(Guid apoliceId) => new(
        Codigo: "apolice-nao-encontrada",
        Titulo: "Apólice não encontrada",
        Detalhe: $"Nenhuma apólice foi encontrada com o Id '{apoliceId}'.",
        Tipo: TipoErro.NaoEncontrado,
        Metadados: new Dictionary<string, object?> { ["apoliceId"] = apoliceId });

    public static readonly Erro NumeroObrigatorio = new(
        Codigo: "apolice-numero-obrigatorio",
        Titulo: "Número da apólice obrigatório",
        Detalhe: "O número da apólice é obrigatório.",
        Tipo: TipoErro.Validacao);

    public static readonly Erro SemCobertura = new(
        Codigo: "apolice-sem-cobertura",
        Titulo: "Apólice sem cobertura",
        Detalhe: "A apólice deve ter ao menos uma cobertura.",
        Tipo: TipoErro.RegraDeNegocio);
}
