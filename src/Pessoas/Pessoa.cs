namespace WebApi.Pessoas;

public class Pessoa
{
    public Guid Id { get; set; }
    public string Apelido { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public DateTime Nascimento { get; set; }
    public string[] Stack { get; set; } = Array.Empty<string>();
}