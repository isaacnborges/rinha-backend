using Dapper;
using System.Collections.Concurrent;
using System.Text.Json;
using WebApi.Database;

namespace WebApi;

public class InserirPessoaQueueService : BackgroundService
{
    private readonly ConcurrentQueue<Pessoa> _queue;
    private readonly IDbConnectionFactory _connectionFactory;

    private readonly int _maxInsertionsPerBatch = 10;
    private readonly TimeSpan _batchDelay = TimeSpan.FromSeconds(1);

    public InserirPessoaQueueService(ConcurrentQueue<Pessoa> queue, IDbConnectionFactory connectionFactory)
    {
        _queue = queue;
        _connectionFactory = connectionFactory;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var insertionsInBatch = 0;

            while (insertionsInBatch < _maxInsertionsPerBatch && _queue.TryDequeue(out var pessoa))
            {
                await InserirPessoaAsync(pessoa);
                insertionsInBatch++;
            }

            await Task.Delay(_batchDelay, stoppingToken);
        }
    }


    private async Task InserirPessoaAsync(Pessoa pessoa)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
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
}