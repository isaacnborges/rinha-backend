using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;
using WebApi;
using WebApi.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNpgsqlDataSource(builder.Configuration.GetConnectionString("DatabaseConnection")!, ServiceLifetime.Scoped);
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new PostgreSqlConnectionFactory(builder.Configuration.GetConnectionString("DatabaseConnection")!));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("RedisConnection")!));
builder.Services.AddSingleton<ConcurrentQueue<Pessoa>>();
builder.Services.AddHostedService<InserirPessoaQueueService>();

builder.Services.AddSingleton<DatabaseInitializer>();

var app = builder.Build();

app.MapPost("/pessoas", async (
    PessoaRequest request, 
    [FromServices] NpgsqlConnection connection, 
    [FromServices] IConnectionMultiplexer multiplexer,
    [FromServices] ConcurrentQueue<Pessoa> pessoaQueue) =>
{
    if (PessoaRequest.IsInvalidRequest(request))
        return Results.UnprocessableEntity();

    var pessoa = PessoaRequest.ToEntity(request);

    var cache = multiplexer.GetDatabase();
    var apelidoUsado = await cache.StringGetAsync(pessoa.Apelido);
    if (apelidoUsado.HasValue)
        return Results.UnprocessableEntity();

    try
    {
        await cache.StringSetAsync(pessoa.Id.ToString(), JsonSerializer.Serialize(pessoa));
        await cache.StringSetAsync(pessoa.Apelido, ".");

        pessoaQueue.Enqueue(pessoa);
    }
    catch (Exception)
    {
        return Results.UnprocessableEntity();
    }

    return Results.Created($"/pessoas/{pessoa.Id}", pessoa);
});

app.MapGet("/pessoas/{id}", async (
    Guid id, 
    [FromServices] NpgsqlConnection connection, 
    [FromServices] IConnectionMultiplexer multiplexer) =>
{
    var cache = multiplexer.GetDatabase();
    var cachedResult = await cache.StringGetAsync(id.ToString());
    if (!cachedResult.IsNullOrEmpty)
    {
        var cachedPessoa = JsonSerializer.Deserialize<Pessoa>(cachedResult!);
        return Results.Ok(cachedPessoa);
    }

    var pessoa = await connection.QuerySingleOrDefaultAsync<Pessoa>(
        @"
            SELECT Id, Apelido, Nome, Nascimento, Stack
            FROM Pessoas 
            WHERE Id = @Id 
            LIMIT 1",
            new { Id = id }
        );

    return pessoa is null 
        ? Results.NotFound() 
        : Results.Ok(pessoa);
});

app.MapGet("/pessoas", async (
    string t, 
    [FromServices] NpgsqlConnection connection, 
    [FromServices] IConnectionMultiplexer multiplexer) =>
{
    if (string.IsNullOrWhiteSpace(t))
        return Results.BadRequest();

    var cache = multiplexer.GetDatabase();
    var cacheKey = $"PessoasSearch:{t}";
    var cachedResults = await cache.StringGetAsync(cacheKey);
    if (!string.IsNullOrEmpty(cachedResults))
    {
        var pessoasCache = JsonSerializer.Deserialize<IEnumerable<Pessoa>>(cachedResults!);
        return Results.Ok(pessoasCache);
    }

    var sql = @"
            SELECT Id, Apelido, Nome, Nascimento, Stack::text
            FROM Pessoas
            WHERE Apelido LIKE @Term
               OR Nome LIKE @Term
               OR to_jsonb(Stack)::text LIKE @Term
            LIMIT 50";

    var pessoas = await connection.QueryAsync<Pessoa>(sql, new { Term = $"%{t}%" });

    var serializedResults = JsonSerializer.Serialize(pessoas);
    await cache.StringSetAsync(cacheKey, serializedResults);

    return Results.Ok(pessoas);
});

app.MapGet("/contagem-pessoas", async ([FromServices] NpgsqlConnection connection) =>
{
    return Results.Ok(await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Pessoas"));
});

var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.Run();