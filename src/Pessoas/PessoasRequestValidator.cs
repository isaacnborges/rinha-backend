using FluentValidation;
using Microsoft.Extensions.Caching.Distributed;
using WebApi.Extensions;
using WebApi.Repositories;

namespace WebApi.Pessoas;

public class PessoasRequestValidator : AbstractValidator<PessoasRequest>
{
    private readonly IPessoaRepository _pessoaRepository;
    private readonly IDistributedCache _distributedCache;

    public PessoasRequestValidator(IPessoaRepository pessoaRepository, IDistributedCache distributedCache)
    {
        _pessoaRepository = pessoaRepository;
        _distributedCache = distributedCache;

        RuleFor(p => p.Apelido)
            .NotEmpty().WithMessage("[Apelido] não pode ser nulo")
            .MaximumLength(32).WithMessage("[Apelido] não pode conter mais de 32 caracteres")
            .MustAsync(BeUniqueApelido).WithMessage("O apelido já está em uso.");

        RuleFor(p => p.Nome)
            .NotEmpty().WithMessage("[Nome] não pode ser nulo")
            .MaximumLength(100).WithMessage("[Apelido] não pode conter mais de 100 caracteres");

        RuleFor(p => p.Nascimento)
            .NotEmpty().WithMessage("[Nascimento] não pode ser nulo")
            .Must(BeValidDate).WithMessage("[Nascimento] inválida.");

        RuleForEach(p => p.Stack)
            .NotEmpty().WithMessage("[Stack] não pode ser vazio")
            .MaximumLength(32).WithMessage("[Stack] não pode conter mais de 32 caracteres");
    }

    private bool BeValidDate(string date) => 
        DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _);

    private async Task<bool> BeUniqueApelido(string apelido, CancellationToken cancellationToken)
    {
        var pessoa = await _distributedCache.GetCacheAsync(apelido, () => _pessoaRepository.GetByApelidoAsync(apelido));
        return pessoa is null;
    }
}