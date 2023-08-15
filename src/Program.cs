using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;
using WebApi;
using WebApi.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNpgsqlDataSource(builder.Configuration.GetConnectionString("DatabaseConnection")!, ServiceLifetime.Scoped);
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new PostgreSqlConnectionFactory(builder.Configuration.GetConnectionString("DatabaseConnection")!));
builder.Services.AddSingleton<DatabaseInitializer>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapPost("/pessoas", async (PessoaRequest request, [FromServices] NpgsqlConnection connection) =>
{
    if (PessoaRequest.IsInvalidRequest(request))
        return Results.UnprocessableEntity();

    var pessoa = PessoaRequest.ToEntity(request);

    try
    {
        await connection.ExecuteAsync(
            @"
                INSERT INTO Pessoas (Id, Apelido, Nome, Nascimento, Stack)
                VALUES (@Id, @Apelido, @Nome, @Nascimento, @Stack::jsonb)",
            new
            {
                pessoa.Id,
                pessoa.Apelido,
                pessoa.Nome,
                pessoa.Nascimento,
                Stack = JsonSerializer.Serialize(pessoa.Stack)
            });
    }
    catch (PostgresException pEx)
    {
        if (pEx?.ConstraintName == "pessoas_apelido_key")
            return Results.UnprocessableEntity();
    }
    catch (Exception ex)
    {
        return Results.UnprocessableEntity(ex.Message);
    }

    return Results.Created($"/pessoas/{pessoa.Id}", pessoa);
});

app.MapGet("/pessoas/{id}", async (Guid id, [FromServices] NpgsqlConnection connection) =>
{
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

app.MapGet("/pessoas", async (string t, [FromServices] NpgsqlConnection connection) =>
{
    if (string.IsNullOrWhiteSpace(t))
        return Results.BadRequest();

    var sql = @"
            SELECT Id, Apelido, Nome, Nascimento, Stack::text
            FROM Pessoas
            WHERE Apelido LIKE @Term
               OR Nome LIKE @Term
               OR to_jsonb(Stack)::text LIKE @Term
            LIMIT 50";

    var pessoas = await connection.QueryAsync<Pessoa>(sql, new { Term = $"%{t}%" });

    return Results.Ok(pessoas);
});

app.MapGet("/contagem-pessoas", ([FromServices] NpgsqlConnection connection) =>
{
    return Results.Ok(connection.ExecuteScalar<int>("SELECT COUNT(1) FROM Pessoas"));
});

var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.Run();