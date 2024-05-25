using APIHorario.Models;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo()
    {
        Title = "APIHorario",
        Description = "APIHorario - Testando Output Cache middleware com Minimal APIs",
        Version = "v1"
    });
});

// Documentacao - OutputCache com Redis:
// https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output?preserve-view=true&view=aspnetcore-8.0#redis-cache
builder.Services.AddStackExchangeRedisOutputCache(
    options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = nameof(APIHorario);
    });

builder.Services.AddOutputCache(options =>
{
    options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(20);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/nocache", () =>
{
    var result = new Resultado() { Mensagem = "Teste sem cache" };
    app.Logger.LogInformation($"{result.HorarioAtual} {result.Mensagem}");
    return result;
}).WithOpenApi();

app.MapGet("/cache", () =>
{
    Thread.Sleep(5000); // Simulando uma consulta demorada
    var result = new Resultado() { Mensagem = "Teste com cache" };
    app.Logger.LogInformation($"{result.HorarioAtual} {result.Mensagem}");
    return result;
}).WithOpenApi().CacheOutput();

app.MapGet("/cachequerystring", (string valorTeste) =>
{
    Thread.Sleep(5000); // Simulando uma consulta demorada
    var result = new Resultado()
    {
        Mensagem = $"Teste com cache | Query string: {nameof(valorTeste)} = {valorTeste}"
    };
    app.Logger.LogInformation($"{result.HorarioAtual} {result.Mensagem}");
    return result;
})
.WithOpenApi()
.CacheOutput(policy =>
{
    policy.SetVaryByQuery(["valorTeste"]);
    policy.Expire(TimeSpan.FromSeconds(15));
});

app.UseOutputCache();

app.Run();