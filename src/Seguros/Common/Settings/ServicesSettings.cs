namespace Seguros.Common.Settings;

/// <summary>
/// Endereço HTTP de cada bounded context. Hoje todos rodam no mesmo processo, então em
/// Development o endereço de Apólices é a própria aplicação. Se um contexto for extraído para
/// um serviço próprio, basta trocar o endereço aqui (ou na variável de ambiente) — nenhum código
/// do contexto consumidor muda.
/// </summary>
public sealed class ServicesSettings
{
    public const string SectionName = "ServicesSettings";

    public string ApolicesUri { get; set; } = string.Empty;

    /// <summary>
    /// Garante a barra final: sem ela, um endereço com caminho (ex.: atrás de um gateway,
    /// <c>https://gateway/seguros</c>) perderia esse caminho ao combinar com a rota relativa.
    /// </summary>
    public static Uri ComBarraFinal(string url) => new(url.EndsWith('/') ? url : url + "/");

    public static bool EhUrlHttpAbsoluta(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
