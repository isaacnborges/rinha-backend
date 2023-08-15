namespace WebApi.Repositories;

public interface IPessoaRepository
{
    Task AddAsync(Pessoa pessoa);

    Task<Pessoa> GetByIdAsync(Guid id);

    Task<Pessoa> GetByApelidoAsync(string apelido);

    Task<IEnumerable<Pessoa>> GetByTermAsync(string term);

    Task<int> CountAsync();
}
