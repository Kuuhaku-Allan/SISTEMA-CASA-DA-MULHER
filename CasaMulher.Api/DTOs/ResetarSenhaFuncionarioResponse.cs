namespace CasaMulher.Api.DTOs;

public class ResetarSenhaFuncionarioResponse
{
    public string Mensagem { get; set; } = string.Empty;

    public string SenhaTemporaria { get; set; } = string.Empty;

    public bool DeveTrocarSenha { get; set; }
}
