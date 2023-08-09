namespace WebApi.Pessoas;

public record PessoasRequest
{
    public string? Apelido { get; set; }
    public string? Nome { get; set; }
    public string Nascimento { get; set; } = string.Empty;
    public string[]? Stack { get; set; }

    public Pessoa ToEntity(PessoasRequest request)
    {
        return new Pessoa
        {
            Id = Guid.NewGuid(),
            Apelido = request.Apelido,
            Nome = request.Nome,
            Nascimento = DateTime.Parse(request.Nascimento, default),
            Stack = request.Stack ?? Array.Empty<string>()
        };
    }
}
