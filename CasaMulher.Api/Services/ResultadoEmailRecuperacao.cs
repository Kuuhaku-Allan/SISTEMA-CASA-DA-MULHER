namespace CasaMulher.Api.Services;

public sealed record ResultadoEmailRecuperacao(
    bool EmailEnviado,
    string? StatusEmail,
    string? AvisoEmail,
    string? LinkConfirmacaoDesenvolvimento)
{
    public static ResultadoEmailRecuperacao SemEmail()
    {
        return new ResultadoEmailRecuperacao(
            false,
            "Não enviado",
            "Informe um e-mail de recuperação válido.",
            null);
    }

    public static ResultadoEmailRecuperacao SemBaseUrl()
    {
        return new ResultadoEmailRecuperacao(
            false,
            "Não enviado",
            "Para enviar confirmação de e-mail de recuperação, configure Frontend:BaseUrl.",
            null);
    }
}
