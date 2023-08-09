using Dapper;
using WebApi.Database;
using WebApi.Pessoas;

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
                WHERE Id = @Id LIMIT 1", new { Id = id });
    }

    public async Task<IEnumerable<Pessoa>> GetByTermAsync(string term)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = @"
            SELECT DISTINCT p.*
            FROM Pessoas p
            JOIN UNNEST(p.Stack) AS s ON true
            WHERE LOWER(Apelido) LIKE LOWER(@Term) OR LOWER(Nome) LIKE LOWER(@Term) OR LOWER(s) LIKE LOWER(@Term)
            LIMIT 50";

        return await connection.QueryAsync<Pessoa>(sql, new { Term = $"%{term}%" });
    }

    public async Task<int> CountAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Pessoas");
    }
}
