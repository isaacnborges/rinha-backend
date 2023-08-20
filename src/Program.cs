using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;
using WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(builder.Configuration.GetConnectionString("DatabaseConnection")));
builder.Services.AddScoped<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("RedisConnection")!));

var app = builder.Build();

app.MapPost("/pessoas", async (PessoaRequest request, [FromServices] IDbConnection dbConnection, [FromServices] IConnectionMultiplexer multiplexer) =>
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
        await dbConnection.ExecuteAsync(@"
                INSERT INTO Pessoas (id, Apelido, Nome, Nascimento, Stack) 
                VALUES (@Id, @Apelido, @Nome, @Nascimento, @Stack)", pessoa);

        await cache.StringSetAsync(pessoa.Id.ToString(), JsonSerializer.Serialize(pessoa));
        await cache.StringSetAsync(pessoa.Apelido, ".");
    }
    catch (Exception)
    {
        return Results.UnprocessableEntity();
    }

    return Results.Created($"/pessoas/{pessoa.Id}", pessoa);
});

app.MapGet("/pessoas/{id}", async (Guid id, [FromServices] IDbConnection dbConnection, [FromServices] IConnectionMultiplexer multiplexer) =>
{
    var cache = multiplexer.GetDatabase();
    var cachedResult = await cache.StringGetAsync(id.ToString());
    if (!cachedResult.IsNullOrEmpty)
    {
        var cachedPessoa = JsonSerializer.Deserialize<Pessoa>(cachedResult!);
        return Results.Ok(cachedPessoa);
    }

    var pessoa = await dbConnection.QueryFirstOrDefaultAsync<Pessoa>(
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

app.MapGet("/pessoas", async (string t, [FromServices] IDbConnection dbConnection) =>
{
    if (string.IsNullOrWhiteSpace(t))
        return Results.BadRequest();

    var pessoas = await dbConnection.QueryAsync<Pessoa>(
        $@"SELECT Id, Apelido, Nome, Nascimento, Stack FROM Pessoas
                WHERE search ILIKE '%' || @Term || '%'
                LIMIT 50", new
        {
            Term = t?.Replace(' ', '%')
        });

    return Results.Ok(pessoas);
});

app.MapGet("/contagem-pessoas", async ([FromServices] IDbConnection dbConnection) =>
{
    return Results.Ok(await dbConnection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Pessoas"));
});

app.Run();