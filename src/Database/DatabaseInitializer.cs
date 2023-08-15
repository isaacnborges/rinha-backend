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
            Id UUID PRIMARY KEY NOT NULL,
            Apelido VARCHAR(32) UNIQUE NOT NULL,
            Nome VARCHAR(100) NOT NULL,
            Nascimento DATE NOT NULL,
            Stack JSONB
        );

        CREATE INDEX IF NOT EXISTS term_search_index_apelido ON pessoas
            USING gin(to_tsvector('english', Apelido));

        CREATE INDEX IF NOT EXISTS term_search_index_nome ON pessoas
            USING gin(to_tsvector('english', Nome));

        CREATE INDEX IF NOT EXISTS term_search_index_stack ON pessoas
            USING gin(to_tsvector('english', Stack));");
    }
}