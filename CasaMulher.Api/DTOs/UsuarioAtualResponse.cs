namespace CasaMulher.Api.DTOs;

public class UsuarioAtualResponse
{
    public string NomeCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;

    public string IdentificadorFuncionario { get; set; } = string.Empty;

    public bool DoisFatoresObrigatorio { get; set; }

    public bool DoisFatoresAtivado { get; set; }

    public bool DeveTrocarSenha { get; set; }
}
