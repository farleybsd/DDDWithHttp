using FastEndpoints;
using FastEndpoints.OpenApi;
using Seguros.Bootstrap;
using Seguros.Common.Erros;
using Seguros.Domain.Apolices.Infrastructure;
using Seguros.Domain.Sinistros.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddProblemDetailsRfc9457()
    .AddDocumentation()
    .AddSettings(builder.Configuration)
    // Cada bounded context registra as próprias dependências no seu módulo.
    .AddApolicesModule()
    .AddSinistrosModule();

var app = builder.Build();

app.UseProblemDetailsRfc9457();
app.UseFastEndpoints(c => c.Errors.UseProblemDetailsRfc9457());
app.MapOpenApi();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Seguros API v1"));

app.Run();

public partial class Program;
