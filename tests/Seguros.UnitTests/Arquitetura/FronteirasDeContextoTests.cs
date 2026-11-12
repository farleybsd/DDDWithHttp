using System.Reflection;
using Seguros.Domain.Apolices.Aggregates.Apolices;

namespace Seguros.UnitTests.Arquitetura;

/// <summary>
/// Os dois contextos vivem no mesmo assembly, então o compilador não impede que um use os tipos
/// do outro. Este teste faz esse papel: nenhum tipo de um contexto pode aparecer na assinatura
/// (herança, campos, propriedades, parâmetros, retornos) de um tipo do outro. A única forma de
/// Sinistros saber algo de Apólices é pelo contrato HTTP.
/// </summary>
/// <remarks>
/// Por ser baseado em reflexão, olha assinaturas e não o corpo dos métodos — é uma rede de
/// segurança, não uma prova.
/// </remarks>
public class FronteirasDeContextoTests
{
    private const string Apolices = "Seguros.Domain.Apolices";
    private const string Sinistros = "Seguros.Domain.Sinistros";

    private static readonly Assembly Aplicacao = typeof(Apolice).Assembly;

    [Theory]
    [InlineData(Sinistros, Apolices)]
    [InlineData(Apolices, Sinistros)]
    public void ContextoNaoReferenciaTiposDoOutroContexto(string origem, string proibido)
    {
        var violacoes = Aplicacao.GetTypes()
            .Where(t => EstaNoContexto(t, origem))
            .SelectMany(t => TiposReferenciados(t).Where(r => EstaNoContexto(r, proibido)).Select(r => $"{t.FullName} -> {r.FullName}"))
            .Distinct()
            .ToList();

        Assert.True(violacoes.Count == 0, "Referências entre contextos:\n" + string.Join("\n", violacoes));
    }

    private static bool EstaNoContexto(Type tipo, string contexto) =>
        tipo.Namespace is { } ns && (ns == contexto || ns.StartsWith(contexto + "."));

    private static IEnumerable<Type> TiposReferenciados(Type tipo)
    {
        const BindingFlags todos = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                   BindingFlags.Static | BindingFlags.DeclaredOnly;

        var diretos = new List<Type>();
        if (tipo.BaseType is not null) diretos.Add(tipo.BaseType);
        diretos.AddRange(tipo.GetInterfaces());
        diretos.AddRange(tipo.GetFields(todos).Select(f => f.FieldType));
        diretos.AddRange(tipo.GetProperties(todos).Select(p => p.PropertyType));
        diretos.AddRange(tipo.GetConstructors(todos).SelectMany(c => c.GetParameters()).Select(p => p.ParameterType));
        foreach (var metodo in tipo.GetMethods(todos))
        {
            diretos.Add(metodo.ReturnType);
            diretos.AddRange(metodo.GetParameters().Select(p => p.ParameterType));
        }

        return diretos.SelectMany(Expandir);
    }

    /// <summary>Inclui argumentos genéricos e tipos de elemento (List&lt;X&gt;, X[], Task&lt;X&gt;...).</summary>
    private static IEnumerable<Type> Expandir(Type tipo)
    {
        yield return tipo;

        if (tipo.HasElementType)
            foreach (var t in Expandir(tipo.GetElementType()!))
                yield return t;

        if (tipo.IsGenericType)
            foreach (var argumento in tipo.GetGenericArguments())
                foreach (var t in Expandir(argumento))
                    yield return t;
    }
}
