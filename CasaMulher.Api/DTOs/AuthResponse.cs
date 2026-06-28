namespace CasaMulher.Api.DTOs;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTime? ExpiraEm { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;

    public string? ProfessorCurso { get; set; }

    public string IdentificadorFuncionario { get; set; } = string.Empty;

    public bool RequerDoisFatores { get; set; }

    public string? LoginTemporario { get; set; }

    public bool DoisFatoresObrigatorio { get; set; }

    public bool DoisFatoresAtivado { get; set; }

    public bool DeveTrocarSenha { get; set; }

    public bool SecuritySetupRequired { get; set; }
}
