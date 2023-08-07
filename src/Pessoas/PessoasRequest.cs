namespace WebApi.Pessoas;

public record PessoasRequest
{
    public string Apelido { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Nascimento { get; set; } = string.Empty;
    public string[] Stack { get; set; } = Array.Empty<string>();

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
