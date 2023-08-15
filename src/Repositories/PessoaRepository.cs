using Dapper;
using WebApi.Database;

namespace WebApi.Repositories;

public class PessoaRepository : IPessoaRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PessoaRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(Pessoa pessoa)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(
            @"
                INSERT INTO Pessoas (Id, Apelido, Nome, Nascimento, Stack)
                VALUES (@Id, @Apelido, @Nome, @Nascimento, @Stack)",
            pessoa);
    }

    public async Task<Pessoa> GetByIdAsync(Guid id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Pessoa>(
            @"
                SELECT Id, Apelido, Nome, Nascimento, Stack
                FROM Pessoas 
                WHERE Id = @Id 
                LIMIT 1", 
                new { Id = id }
            );
    }

    public async Task<Pessoa> GetByApelidoAsync(string apelido)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Pessoa>(
            @"
                SELECT Id, Apelido, Nome, Nascimento, Stack
                FROM Pessoas 
                WHERE Apelido = @Apelido
                LIMIT 1",
                new { Apelido = apelido }
            );
    }

    public async Task<IEnumerable<Pessoa>> GetByTermAsync(string term)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = @"
            SELECT Id, Apelido, Nome, Nascimento, Stack
              FROM Pessoas
            WHERE to_tsvector('english', Apelido) @@ to_tsquery(@term)
               OR to_tsvector('english', Nome) @@ to_tsquery(@term)
               OR to_tsvector(array_to_string(Stack, ' ')) @@ to_tsquery(@term)
            LIMIT 50";

        var pessoas = await connection.QueryAsync<Pessoa>(sql, new { Term = $"'${term}:*'" });

        return pessoas;
    }

    public async Task<int> CountAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Pessoas");
    }
}
