using FastEndpoints;
using FastEndpoints.OpenApi;
using Sinistros.Api.Domain;
using Sinistros.Api.Infrastructure;
using Sinistros.Api.Infrastructure.ApolicesApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFastEndpoints().OpenApiDocument(o => o.DocumentName = "v1");
builder.Services.AddSingleton<ISinistroRepository, InMemorySinistroRepository>();
builder.Services.AddScoped<Sinistros.Api.Features.RegistrarSinistro.Handler>();
builder.Services.AddScoped<Sinistros.Api.Features.ConsultarSinistro.Handler>();
builder.Services.AddScoped<Sinistros.Api.Features.ListarSinistrosPorApolice.Handler>();
builder.Services.AddScoped<Sinistros.Api.Features.ConsultarApoliceDoSinistro.Handler>();

builder.Services.AddApolicesApiClient(builder.Configuration);

var app = builder.Build();

app.UseFastEndpoints();
app.MapOpenApi();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Sinistros API v1"));

app.Run();

public partial class Program;
