using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using WebApi.Database;
using WebApi.Extensions;
using WebApi.Pessoas;
using WebApi.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDbConnectionFactory>(_ => new PostgreSqlConnectionFactory(builder.Configuration.GetConnectionString("DatabaseConnection")!));
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddScoped<IPessoaRepository, PessoaRepository>();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "PessoasCache";
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(PessoasRequestValidator).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapPost("/pessoas", async (IPessoaRepository pessoaRepository, IDistributedCache cache, PessoasRequest request) =>
{
    var validator = new PessoasRequestValidator(pessoaRepository, cache);
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(e => e.ErrorMessage).ToArray());

        return Results.UnprocessableEntity(errors);
    }

    var pessoa = request.ToEntity(request);

    try
    {
        await pessoaRepository.AddAsync(pessoa);
    }
    catch (Exception e)
    {
        return Results.UnprocessableEntity(e.Message);
    }

    return Results.Created($"/pessoas/{pessoa.Id}", pessoa);
});

app.MapGet("/pessoas/{id}", async (IPessoaRepository pessoaRepository, IDistributedCache cache, Guid id) =>
{
    var pessoa = await cache.GetCacheAsync(id.ToString(), () => pessoaRepository.GetByIdAsync(id));

    return pessoa is null 
        ? Results.NotFound() 
        : Results.Ok(pessoa);
});

app.MapGet("/pessoas", async (IPessoaRepository pessoaRepository, IDistributedCache cache, string t) =>
{
    var pessoas = await cache.GetCacheAsync(t, () => pessoaRepository.GetByTermAsync(t));

    return Results.Ok(pessoas);
});

app.MapGet("/contagem-pessoas", async (IPessoaRepository pessoaRepository) =>
    Results.Ok(await pessoaRepository.CountAsync()));

var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.Run();