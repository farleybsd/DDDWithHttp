using Apolices.Api.Domain;
using Apolices.Api.Infrastructure;
using FastEndpoints;
using FastEndpoints.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFastEndpoints().OpenApiDocument(o => o.DocumentName = "v1");
builder.Services.AddSingleton<IApoliceRepository, InMemoryApoliceRepository>();
builder.Services.AddScoped<Apolices.Api.Features.ConsultarApolice.Handler>();
builder.Services.AddScoped<Apolices.Api.Features.ListarApolices.Handler>();

var app = builder.Build();

app.UseFastEndpoints();
app.MapOpenApi();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Apólices API v1"));

app.Run();
