using System.Globalization;

namespace WebApi;

public class Pessoa
{
    public Guid Id { get; set; }
    public string Apelido { get; set; }
    public string Nome { get; set; }
    public DateTime Nascimento { get; set; }
    public string[]? Stack { get; init; }
}

public record PessoaRequest()
{
    public string? Apelido { get; set; }
    public string? Nome { get; set; }
    public string? Nascimento { get; set; }
    public IEnumerable<string>? Stack { get; set; }

    internal static bool IsInvalidRequest(PessoaRequest request)
    {
        if (IsInvalidProperties(request))
            return true;

        if (IsInvalidDate(request.Nascimento))
            return true;

        foreach (var item in request.Stack ?? Enumerable.Empty<string>())
            if (IsInvalidStackLength(item))
                return true;

        return false;
    }

    private static bool IsInvalidProperties(PessoaRequest request) =>
        string.IsNullOrEmpty(request.Nascimento)
        || string.IsNullOrEmpty(request.Nome)
        || request.Nome.Length > 100
        || string.IsNullOrEmpty(request.Apelido)
        || request.Apelido.Length > 32;

    private static bool IsInvalidStackLength(string item) => item.Length > 32;

    private static bool IsInvalidDate(string date) =>
        !DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    public static Pessoa ToEntity(PessoaRequest request) => new()
    {
        Id = Guid.NewGuid(),
        Apelido = request.Apelido!,
        Nome = request.Nome!,
        Nascimento = DateTime.Parse(request.Nascimento!, default),
        Stack = request.Stack is not null && request.Stack.Any() ? request.Stack.ToArray() : Array.Empty<string>()
    };
}