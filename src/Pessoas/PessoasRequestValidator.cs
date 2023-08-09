using FluentValidation;
using System.Globalization;

namespace WebApi.Pessoas;

public class PessoasRequestValidator : AbstractValidator<PessoasRequest>
{
    public PessoasRequestValidator()
    {
        RuleFor(p => p.Apelido)
            .NotEmpty().WithMessage("[Apelido] nao pode ser nulo")
            .MaximumLength(32).WithMessage("[Apelido] nao pode conter mais de 32 caracteres");

        RuleFor(p => p.Nome)
            .NotEmpty().WithMessage("[Nome] não pode ser nulo")
            .MaximumLength(100).WithMessage("[Apelido] nao pode conter mais de 100 caracteres");

        RuleFor(p => p.Nascimento)
            .NotEmpty().WithMessage("[Nascimento] nao pode ser nulo")
            .Must(BeValidDate).WithMessage("[Nascimento] invalida");

        RuleForEach(p => p.Stack)
            .NotEmpty().WithMessage("[Stack] nao pode ser vazio")
            .MaximumLength(32).WithMessage("[Stack] nao pode conter mais de 32 caracteres");
    }

    private bool BeValidDate(string date) => 
        DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}