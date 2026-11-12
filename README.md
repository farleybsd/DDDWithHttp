# Seguros V2 — DDD em uma única API, contextos conversando por HTTP

Refatoração do projeto [`DDDWithHttp`](../DDDWithHttp) (duas APIs: `Apolices.Api` e
`Sinistros.Api`) para **uma única aplicação** com os dois bounded contexts lado a lado — no
estilo do *GymErp modernized* (sample05 da Masterclass): um projeto, uma pasta por contexto em
`Domain/`, um módulo de registro por contexto e **comunicação entre contextos por HTTP**, mesmo
rodando no mesmo processo.

A injeção de dependência usa o contêiner nativo do ASP.NET Core. No lugar dos módulos Autofac do
GymErp, cada contexto expõe um método de extensão do `IServiceCollection`:

```csharp
builder.Services
    .AddApolicesModule()
    .AddSinistrosModule();
```

## Por que HTTP dentro do mesmo processo?

Os contextos ficam juntos para simplificar deploy e operação, mas o acoplamento entre eles
continua sendo o de serviços separados:

- Sinistros **não referencia nenhum tipo** de Apólices — só conhece o contrato JSON de
  `GET /apolices/{id}` (garantido pelo teste `FronteirasDeContextoTests`).
- Cada contexto tem seu próprio vocabulário (`TipoCobertura` existe duas vezes, de propósito) e a
  tradução acontece no adaptador HTTP (`ApolicesApiClient`, a camada anticorrupção).
- O endereço de cada contexto vem de configuração (`ServicesSettings`). Para extrair Apólices
  para um serviço próprio amanhã, basta mudar `ServicesSettings:ApolicesUri` — nenhuma linha do
  contexto de Sinistros muda.

## Estrutura

```
DDDWithHttpV2.sln
src/Seguros/                              uma única API — porta 5200
  Program.cs                              registra os módulos de cada contexto
  Bootstrap/ServiceExtensions.cs          documentação (OpenAPI) e configurações
  Common/                                 o que é transversal (como o Common do GymErp)
    Result.cs, Erro.cs, TipoErro.cs       Result + erros esperados
    Erros/                                RFC 9457 (ProblemDetails), GlobalExceptionHandler
    Settings/ServicesSettings.cs          endereço HTTP de cada contexto
    Settings/HttpResilienceSettings.cs    limite de concorrência / retry / circuit breaker / timeout
    Http/ContextHttpClientExtensions.cs   AddContextHttpClient<,>: HttpClient tipado + resiliência
  Domain/
    Apolices/                             ── contexto de Apólices (não conhece nenhum outro)
      Aggregates/Apolices/                Apolice, Segurado, PeriodoVigencia, Cobertura,
                                          TipoCobertura, ErrosDeApolice, IApoliceRepository
      Contracts/ApoliceResponse.cs        contrato HTTP público do contexto
      Features/ConsultarApolice/          GET /apolices/{id}
      Features/ListarApolices/            GET /apolices
      Infrastructure/
        ApolicesModule.cs                 AddApolicesModule(): registros do contexto
        InMemoryApoliceRepository.cs
    Sinistros/                            ── contexto de Sinistros (conhece Apólices só por HTTP)
      Aggregates/Sinistros/               Sinistro, StatusSinistro, TipoCobertura (próprio),
                                          ApoliceReferenciada (VO traduzido), ErrosDeSinistro,
                                          ISinistroRepository, IApolicesApiClient (porta)
      Contracts/                          SinistroResponse, ApoliceDoSinistroResponse
      Features/RegistrarSinistro/         POST /sinistros
      Features/ConsultarSinistro/         GET /sinistros/{id}
      Features/ListarSinistrosPorApolice/ GET /apolices/{apoliceId}/sinistros
      Features/ConsultarApoliceDoSinistro/ GET /sinistros/{id}/apolice
      Infrastructure/
        SinistrosModule.cs                AddSinistrosModule(): registros do contexto +
                                          cliente HTTP Sinistros → Apólices
        InMemorySinistroRepository.cs
        ApolicesApi/                      ApolicesApiClient (adaptador HTTP) + ApoliceApiDto
tests/
  Seguros.UnitTests/                      domínio dos dois contextos + fronteira entre contextos
  Seguros.IntegrationTests/               a aplicação inteira via WebApplicationFactory
http/Seguros.http                         requisições prontas (REST Client / Rider / VS)
```

### O que mudou em relação à V1

| V1 (duas APIs) | V2 (uma API) |
|---|---|
| `Apolices.Api` (5201) + `Sinistros.Api` (5301) | `Seguros` (5200) |
| `Domain/`, `Features/`, `Infrastructure/` por projeto | `Domain/<Contexto>/{Aggregates,Features,Infrastructure}` |
| Registro de DI no `Program.cs` de cada API | `AddApolicesModule()` / `AddSinistrosModule()` (DI nativo) |
| `Result`, `Erro`, ProblemDetails duplicados em cada API | uma vez só, em `Common/` |
| Seção `ApolicesApi` (endereço + resiliência) | `ServicesSettings` (endereços) + `HttpResilience` |
| Resiliência configurada só para o cliente de Apólices | `AddContextHttpClient<,>` reutilizável por qualquer contexto |
| Dois projetos de inicialização | F5 sobe tudo |

Regras de negócio, contratos HTTP, códigos de erro (`type`/`code`) e dados fictícios são os
mesmos da V1.

## Fluxo

1. `POST /sinistros` chega ao contexto de Sinistros.
2. `RegistrarSinistro.Handler` chama a porta `IApolicesApiClient`.
3. `ApolicesApiClient` faz `GET {ServicesSettings:ApolicesUri}/apolices/{id}` — em Development,
   `http://localhost:5200/apolices/{id}`, ou seja, a própria aplicação. A requisição passa pelo
   pipeline HTTP inteiro e é atendida pelo endpoint `ConsultarApolice` do contexto de Apólices.
4. O JSON volta, é traduzido para `ApoliceReferenciada` e o agregado `Sinistro` valida
   vigência e cobertura: `201`, `404`, `422` ou `503` (Apólices inacessível).

No console dá para ver a chamada:

```
info: System.Net.Http.HttpClient.IApolicesApiClient.ClientHandler[100]
      Sending HTTP request GET http://localhost:5200/apolices/11111111-1111-1111-1111-111111111111
```

## Como é feita a chamada HTTP entre os contextos

A chamada Sinistros → Apólices é montada em 6 peças. Cada uma tem uma responsabilidade só, e
nenhuma delas faz o contexto de Sinistros depender de um tipo do contexto de Apólices.

```
 Sinistros                                                        Apólices
 ─────────────────────────────────────────────────────            ──────────────────────────
 RegistrarSinistro.Handler                                         ConsultarApolice.Endpoint
   │ (1) IApolicesApiClient   ← porta, no domínio                   GET /apolices/{id}
   ▼                                                                   ▲
 ApolicesApiClient            ← (2) adaptador HTTP + tradução          │
   │ httpClient.GetAsync("apolices/{id}")                              │
   ▼                                                                   │
 HttpClient tipado            ← (3) BaseAddress = ServicesSettings:ApolicesUri
   │ Limite → Retry → CB → Timeout       ← (4) pipeline de resiliência │
   ▼                                                                   │
 http://localhost:5200/apolices/{id} ── loopback, pipeline ASP.NET inteiro ┘
                       (mesmo processo, mesma porta)
```

### 1. A porta — o domínio de Sinistros diz *o que* precisa

`Domain/Sinistros/Aggregates/Sinistros/IApolicesApiClient.cs`

```csharp
public interface IApolicesApiClient
{
    Task<ApoliceConsultaResultado> ConsultarApoliceAsync(Guid apoliceId, CancellationToken cancellationToken);
}
```

O resultado já vem no vocabulário de Sinistros e diferencia os três desfechos possíveis, para o
handler escolher o status HTTP certo:

```csharp
public abstract record ApoliceConsultaResultado
{
    public sealed record Encontrada(ApoliceReferenciada Apolice) : ApoliceConsultaResultado; // → segue o fluxo
    public sealed record NaoEncontrada : ApoliceConsultaResultado;                           // → 404
    public sealed record Indisponivel(string Motivo) : ApoliceConsultaResultado;             // → 503
}
```

### 2. O adaptador HTTP — a camada anticorrupção

`Domain/Sinistros/Infrastructure/ApolicesApi/ApolicesApiClient.cs` implementa a porta. Ele é o
**único** ponto que conhece o formato do JSON de Apólices (`ApoliceApiDto`, uma cópia do contrato,
não o tipo `ApoliceResponse` do outro contexto):

```csharp
public sealed class ApolicesApiClient(HttpClient httpClient, ILogger<ApolicesApiClient> logger) : IApolicesApiClient
{
    public async Task<ApoliceConsultaResultado> ConsultarApoliceAsync(Guid apoliceId, CancellationToken ct)
    {
        try
        {
            // rota relativa, sem barra inicial: preserva um eventual caminho no endereço base
            using var response = await httpClient.GetAsync($"apolices/{apoliceId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new ApoliceConsultaResultado.NaoEncontrada();     // resposta de negócio

            if (!response.IsSuccessStatusCode)
                return new ApoliceConsultaResultado.Indisponivel(...);   // 5xx depois dos retries

            var dto = await response.Content.ReadFromJsonAsync<ApoliceApiDto>(ct);
            return new ApoliceConsultaResultado.Encontrada(Traduzir(dto)); // DTO → ApoliceReferenciada
        }
        catch (TimeoutRejectedException) { /* → Indisponivel */ }
        catch (BrokenCircuitException)   { /* → Indisponivel, nem tentou a rede */ }
        catch (HttpRequestException)     { /* → Indisponivel */ }
    }
}
```

`Traduzir` converte `"Colisao"` (texto do contrato de Apólices) para o enum `TipoCobertura` **de
Sinistros**. Se Apólices mudar sua representação interna, só esse método muda.

### 3. O endereço — vem de configuração, nunca do código

`Common/Settings/ServicesSettings.cs` guarda o endereço de cada contexto:

```json
// appsettings.Development.json
"ServicesSettings": { "ApolicesUri": "http://localhost:5200" }
```

Em Development é o endereço da própria aplicação. O valor é validado na subida
(`ValidateOnStart`). Se faltar, a aplicação nem inicia.

### 4. O HttpClient tipado com resiliência — reutilizável por qualquer contexto

`Common/Http/ContextHttpClientExtensions.cs` cria um `HttpClient` tipado via `IHttpClientFactory`,
define o `BaseAddress` a partir de `ServicesSettings` e aplica o pipeline de
`HttpResilienceSettings`:

```csharp
public static IHttpClientBuilder AddContextHttpClient<TClient, TImplementation>(
    this IServiceCollection services, string nomeDoPipeline, Func<ServicesSettings, string> enderecoDoContexto)
{
    var builder = services.AddHttpClient<TClient, TImplementation>((sp, client) =>
    {
        var settings = sp.GetRequiredService<IOptions<ServicesSettings>>().Value;
        client.BaseAddress = ServicesSettings.ComBarraFinal(enderecoDoContexto(settings));
    });

    builder.AddResilienceHandler(nomeDoPipeline, (pipeline, context) =>
        pipeline.AddConcurrencyLimiter(...).AddRetry(...).AddCircuitBreaker(...).AddTimeout(...));

    return builder;
}
```

### 5. O registro — cada contexto declara de quem é cliente

`Domain/Sinistros/Infrastructure/SinistrosModule.cs`, junto com os demais registros do contexto:

```csharp
public static IServiceCollection AddSinistrosModule(this IServiceCollection services)
{
    services.AddSingleton<ISinistroRepository, InMemorySinistroRepository>();
    services.AddScoped<Features.RegistrarSinistro.Handler>();
    // ...

    services.AddContextHttpClient<IApolicesApiClient, ApolicesApiClient>(
        nomeDoPipeline: "sinistros-para-apolices",
        enderecoDoContexto: s => s.ApolicesUri);

    return services;
}
```

Não registre `IApolicesApiClient` de outra forma (por exemplo, `AddScoped<IApolicesApiClient, ...>`).
O registro do `AddHttpClient` é o que entrega o `HttpClient` já configurado, com endereço e
resiliência.

### 6. O uso — o handler só conhece a porta

`Domain/Sinistros/Features/RegistrarSinistro/Handler.cs`:

```csharp
var consulta = await apolicesApiClient.ConsultarApoliceAsync(request.ApoliceId, ct);

return consulta switch
{
    ApoliceConsultaResultado.NaoEncontrada        => ErrosDeSinistro.ApoliceNaoEncontrada(request.ApoliceId), // 404
    ApoliceConsultaResultado.Indisponivel         => ErrosDeSinistro.ApolicesApiIndisponivel,                // 503
    ApoliceConsultaResultado.Encontrada encontrada => await RegistrarAsync(request, encontrada.Apolice, ct)  // 201 / 422
};
```

Do outro lado, o contexto de Apólices não sabe que está sendo chamado por outro contexto. Para
ele, é uma requisição HTTP como qualquer outra em `GET /apolices/{id}`
(`Domain/Apolices/Features/ConsultarApolice/Endpoint.cs`).

### Como adicionar uma nova chamada entre contextos

Exemplo: um futuro contexto de Pagamentos consultando Sinistros.

1. Em `ServicesSettings`, crie `SinistrosUri` e preencha em cada `appsettings.{Ambiente}.json`.
2. No domínio do consumidor, crie a porta (`ISinistrosApiClient`) e o resultado no vocabulário dele.
3. Na `Infrastructure/` do consumidor, crie o DTO (cópia do contrato JSON) e o adaptador.
4. Registre com `services.AddContextHttpClient<ISinistrosApiClient, SinistrosApiClient>("pagamentos-para-sinistros", s => s.SinistrosUri)`.
5. Nunca use `using Seguros.Domain.Sinistros...` dentro de Pagamentos. O
   `FronteirasDeContextoTests` pode ser estendido para garantir isso.

### Vendo a chamada acontecer

Rode a aplicação e faça um `POST /sinistros`. No console aparece:

```
info: System.Net.Http.HttpClient.IApolicesApiClient.LogicalHandler[100]
      Start processing HTTP request GET http://localhost:5200/apolices/11111111-1111-1111-1111-111111111111
info: System.Net.Http.HttpClient.IApolicesApiClient.ClientHandler[101]
      Received HTTP response headers after 42.5564ms - 200
```

Para ver a resiliência, suba com um endereço inacessível
(`$env:ServicesSettings__ApolicesUri = "http://localhost:5999"`). Os logs mostram `OnRetry`,
depois `OnCircuitOpened`, e a API responde `503` sem gravar o sinistro.

## Configuração

```json
"ServicesSettings": { "ApolicesUri": "http://localhost:5200" },   // por ambiente
"HttpResilience": {
  "Timeout": "00:00:02",
  "ConcurrencyLimiter": { "PermitLimit": 50, "QueueLimit": 10 },
  "Retry": { "MaxRetryAttempts": 1, "BaseDelay": "00:00:00.500" },   // 0 desliga o retry
  "CircuitBreaker": { "FailureRatio": 0.5, "SamplingDuration": "00:00:10",
                      "MinimumThroughput": 4, "BreakDuration": "00:00:15" }
}
```

| `ASPNETCORE_ENVIRONMENT` | `ServicesSettings:ApolicesUri` |
|---|---|
| `Development` | `http://localhost:5200` (a própria aplicação) |
| `QAS` | `https://seguros.qas.example/` (placeholder) |
| `Production` | `https://seguros.example/` (placeholder) |

Sobrescreva sem novo build com `ServicesSettings__ApolicesUri`. Sem um endereço http(s) válido a
aplicação **não sobe** (`ValidateOnStart`). Em produção, aponte para o endereço pelo qual a
aplicação é alcançável de dentro dela mesma (nome do serviço/pod, por exemplo) — não
necessariamente o endereço público.

Pipeline de resiliência (mais externo → mais interno):
**Limite de concorrência → Retry → Circuit Breaker → Timeout por tentativa**. O retry só atua em
falhas transitórias (5xx, 408, 429, timeout, erro de rede). `404` é resposta de negócio e não
entra no retry.

### Proteções contra a aplicação sobrecarregar a si mesma

Como os contextos rodam no mesmo processo, cada chamada interna disputa threads e conexões com as
requisições dos clientes. Por isso há dois ajustes em relação à V1:

- **Retry reduzido (padrão 1, era 3).** Aqui a falha mais provável é a própria aplicação estar
  sobrecarregada, e retentar multiplicaria a carga justamente nessa hora: com 3 retries, uma
  requisição viraria até 4 chamadas internas. Com `MaxRetryAttempts: 0` a estratégia de retry
  nem entra no pipeline (o Polly exige no mínimo 1 quando ela existe).
- **Limite de chamadas simultâneas** (`AddConcurrencyLimiter`). No máximo `PermitLimit` chamadas a
  Apólices em andamento ao mesmo tempo, mais `QueueLimit` aguardando. Acima disso a chamada é
  recusada na hora (`RateLimiterRejectedException`), o adaptador a trata como indisponibilidade e
  a API responde `503 apolices-api-indisponivel`. Uma rajada recebe 503 rápido em vez de derrubar
  a aplicação. O limitador fica por fora do retry: o limite vale para a chamada inteira, e uma
  chamada recusada não é retentada.

Os valores podem ser ajustados por ambiente (`HttpResilience__ConcurrencyLimiter__PermitLimit`,
`HttpResilience__Retry__MaxRetryAttempts`). Os testes ficam em `ProtecaoContraSobrecargaTests`.

## Erros (RFC 9457)

Iguais à V1: todo erro sai como `application/problem+json` com `type`, `title`, `status`,
`detail`, `instance`, `code` e `traceId`. Códigos: `validacao` (400), `apolice-nao-encontrada`
(404), `sinistro-nao-encontrado` (404), `apolice-fora-de-vigencia` (422),
`cobertura-nao-contratada` (422), `sinistro-descricao-obrigatoria` (400),
`apolices-api-indisponivel` (503), `erro-interno` (500).

## Como executar

```bash
dotnet run --project src/Seguros     # http://localhost:5200/swagger
```

No Visual Studio, abra `DDDWithHttpV2.sln` e aperte F5. O Swagger agrupa os endpoints pelas tags
`Apolices` e `Sinistros`. Use `http/Seguros.http` para testar.

| Id | Número | Vigência | Coberturas |
|---|---|---|---|
| `11111111-1111-1111-1111-111111111111` | AP-0001 | 2026-01-01 a 2026-12-31 | Colisão, Roubo, Incêndio |
| `22222222-2222-2222-2222-222222222222` | AP-0002 | 2024 (**expirada**) | Colisão |
| `33333333-3333-3333-3333-333333333333` | AP-0003 | 2026-06-01 a 2027-05-31 | Incêndio, Danos a Terceiros |

## Testes

```bash
dotnet test DDDWithHttpV2.sln
```

- **Seguros.UnitTests** — regras de `Apolice`, `PeriodoVigencia` e `Sinistro`, e
  `FronteirasDeContextoTests`, que falha se um contexto usar tipos do outro em assinaturas
  (como os dois estão no mesmo assembly, o compilador não impede isso sozinho).
- **Seguros.IntegrationTests** — a aplicação inteira em memória (`SegurosApiFactory`), em dois
  modos:
  - **contexto de Apólices real** (`ComunicacaoEntreContextosTests`): a requisição HTTP de
    Sinistros é entregue ao próprio `TestServer` e atendida pelos endpoints reais de Apólices;
  - **Apólices simulado** (`ScriptedHttpMessageHandler`): troca só o handler primário do
    `HttpClient`, mantendo o pipeline de resiliência — cobre retry, abertura do circuito,
    timeout, 404/422/503, montagem da URL a partir de `ServicesSettings:ApolicesUri` e os
    contratos de ProblemDetails.

## Simplificações

- Repositórios em memória; dados perdidos ao reiniciar.
- Sem autenticação nem TLS local.
- Comunicação unidirecional: Sinistros → Apólices.
- A chamada HTTP dentro do mesmo processo tem custo (serialização + loopback). É o preço
  consciente de manter os contextos desacoplados como se fossem serviços separados.
