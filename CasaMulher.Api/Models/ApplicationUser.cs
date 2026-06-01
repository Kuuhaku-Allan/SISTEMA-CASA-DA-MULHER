using Microsoft.AspNetCore.Identity;

namespace CasaMulher.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string NomeCompleto { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;

    public string IdentificadorFuncionario { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public bool DeveTrocarSenha { get; set; }

    public bool DoisFatoresObrigatorio { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
