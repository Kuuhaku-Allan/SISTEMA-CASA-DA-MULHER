namespace CasaMulher.Api.DTOs;

public class FuncionarioAdminResponse
{
    public string Id { get; set; } = string.Empty;

    public string IdentificadorFuncionario { get; set; } = string.Empty;

    public string NomeCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;

    public bool Ativo { get; set; }

    public bool DoisFatoresAtivo { get; set; }

    public bool DoisFatoresObrigatorio { get; set; }

    public bool DeveTrocarSenha { get; set; }

    public DateTime CriadoEm { get; set; }
}
