namespace CasaMulher.Api.DTOs;

public class ResetarSenhaFuncionarioResponse
{
    public string Mensagem { get; set; } = string.Empty;

    public bool EmailEnviado { get; set; }

    public string? StatusEmail { get; set; }

    public string? AvisoEmail { get; set; }
}
