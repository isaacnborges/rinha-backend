using Microsoft.EntityFrameworkCore;
using WebApi;
using WebApi.Database;
using WebApi.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDbConnectionFactory>(_ => new PostgreSqlConnectionFactory(builder.Configuration.GetConnectionString("DatabaseConnection")!));
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddTransient<IPessoaRepository, PessoaRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapPost("/pessoas", async (IPessoaRepository pessoaRepository, PessoaRequest request) =>
{
    if (PessoaRequest.IsInvalidRequest(request))
        return Results.UnprocessableEntity();

    var pessoaDb = await pessoaRepository.GetByApelidoAsync(request.Apelido);
    if (pessoaDb is not null)
        return Results.UnprocessableEntity();


    var pessoa = PessoaRequest.ToEntity(request);

    try
    {
        await pessoaRepository.AddAsync(pessoa);
    }
    catch (Exception ex)
    {
        return Results.UnprocessableEntity(ex.Message);
    }

    return Results.Created($"/pessoas/{pessoa.Id}", pessoa);
});

app.MapGet("/pessoas/{id}", async (IPessoaRepository pessoaRepository, Guid id) =>
{
    var pessoa = await pessoaRepository.GetByIdAsync(id);

    return pessoa is null 
        ? Results.NotFound() 
        : Results.Ok(pessoa);
});

app.MapGet("/pessoas", async (IPessoaRepository pessoaRepository, string t) =>
{
    if (string.IsNullOrWhiteSpace(t))
        return Results.BadRequest();

    var pessoas = await pessoaRepository.GetByTermAsync(t);

    return Results.Ok(pessoas);
});

app.MapGet("/contagem-pessoas", async (IPessoaRepository pessoaRepository) =>
    Results.Ok(await pessoaRepository.CountAsync()));

var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.Run();