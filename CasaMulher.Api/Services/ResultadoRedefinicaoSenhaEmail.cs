namespace CasaMulher.Api.Services;

public sealed record ResultadoRedefinicaoSenhaEmail(
    bool EmailEnviado,
    string? StatusEmail,
    string? AvisoEmail)
{
    public static ResultadoRedefinicaoSenhaEmail SemEmail()
    {
        return new ResultadoRedefinicaoSenhaEmail(
            false,
            "NaoConfigurado",
            "Funcionário sem e-mail cadastrado.");
    }

    public static ResultadoRedefinicaoSenhaEmail SemBaseUrl()
    {
        return new ResultadoRedefinicaoSenhaEmail(
            false,
            "NaoConfigurado",
            "Para enviar redefinição de senha por e-mail, configure Frontend:BaseUrl.");
    }
}
