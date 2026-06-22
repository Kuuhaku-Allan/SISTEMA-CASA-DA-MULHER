namespace CasaMulher.Api.DTOs;

public class UsuarioAtualResponse
{
    public string NomeCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? EmailRecuperacao { get; set; }

    public bool EmailRecuperacaoConfirmado { get; set; }

    public string Perfil { get; set; } = string.Empty;

    public string? ProfessorCurso { get; set; }

    public string IdentificadorFuncionario { get; set; } = string.Empty;

    public bool DoisFatoresObrigatorio { get; set; }

    public bool DoisFatoresAtivado { get; set; }

    public bool DeveTrocarSenha { get; set; }
}
