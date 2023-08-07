using Dapper;

namespace WebApi.Database;

public class DatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DatabaseInitializer(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(@"
        CREATE TABLE IF NOT EXISTS Pessoas (
            Id UUID PRIMARY KEY,
            Apelido VARCHAR(32) NOT NULL UNIQUE,
            Nome VARCHAR(100) NOT NULL,
            Nascimento DATE NOT NULL,
            Stack VARCHAR(32)[] )");
    }
}