using Microsoft.AspNetCore.Identity;

namespace CasaMulher.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string NomeCompleto { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;

    public string IdentificadorFuncionario { get; set; } = string.Empty;

    public string? ProfessorCurso { get; set; }

    public bool Ativo { get; set; } = true;

    public bool DeveTrocarSenha { get; set; }

    public bool DoisFatoresObrigatorio { get; set; }

    public bool SecuritySetupRequired { get; set; }

    public string? EmailRecuperacao { get; set; }

    public bool EmailRecuperacaoConfirmado { get; set; }

    public DateTime? EmailRecuperacaoConfirmadoEm { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data da última reconfirmação de credenciais para login por passkey.
    /// Nulo ou com mais de 7 dias exige nova reconfirmação (ID + senha + 2FA se ativo).
    /// </summary>
    public DateTime? PasskeyReconfirmadoEm { get; set; }

    /// <summary>
    /// Data da última atualização da senha via sincronização do portal EQP.
    /// Usada para evitar sobrescrever senha local mais nova com hash antigo do JSON.
    /// </summary>
    public DateTime? EquipeDbPasswordUpdatedAt { get; set; }

    /// <summary>
    /// Versão da senha no portal EQP. Usada para detectar atualizações no JSON.
    /// </summary>
    public int EquipeDbPasswordVersion { get; set; }

    public ICollection<PasskeyCredential> PasskeyCredentials { get; set; } = new List<PasskeyCredential>();

    public ICollection<UserLoginIdentifier> LoginIdentifiers { get; set; } = new List<UserLoginIdentifier>();
}
