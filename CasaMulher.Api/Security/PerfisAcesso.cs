namespace CasaMulher.Api.Security;

public static class PerfisAcesso
{
    public const string Adm = "adm";
    public const string Recepcao = "recepcao";
    public const string Professor = "professor";
    public const string AssistenteSocial = "as_social";
    public const string Juridico = "juridico";

    public static readonly string[] Todos =
    [
        Adm,
        Recepcao,
        Professor,
        AssistenteSocial,
        Juridico
    ];

    public static bool EhValido(string perfil)
    {
        return Todos.Contains(perfil, StringComparer.OrdinalIgnoreCase);
    }
}
