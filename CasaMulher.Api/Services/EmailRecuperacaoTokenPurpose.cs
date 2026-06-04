namespace CasaMulher.Api.Services;

public static class EmailRecuperacaoTokenPurpose
{
    public static string Criar(string emailRecuperacao)
    {
        return $"EmailRecuperacao:{emailRecuperacao.Trim().ToUpperInvariant()}";
    }
}
