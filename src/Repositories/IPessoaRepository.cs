using WebApi.Pessoas;

namespace WebApi.Repositories;

public interface IPessoaRepository
{
    Task AddAsync(Pessoa pessoa);

    Task<Pessoa> GetByIdAsync(Guid id);

    Task<IEnumerable<Pessoa>> GetByTermAsync(string term);

    Task<int> CountAsync();
}
