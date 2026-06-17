using System.ComponentModel.DataAnnotations;
using CasaMulher.Api.Services;

namespace CasaMulher.Api.DTOs;

public class PortalEqpStatusResponse
{
    public bool OAuthConfigurado { get; set; }

    public bool EscritaConfigurada { get; set; }

    public string Organization { get; set; } = string.Empty;

    public string OwnerGitHub { get; set; } = string.Empty;

    public string DbRepository { get; set; } = string.Empty;

    public string DbPath { get; set; } = string.Empty;

    public string Mensagem { get; set; } = string.Empty;
}

public class PortalEqpMeResponse
{
    public bool Logado { get; set; }

    public string? GitHubId { get; set; }

    public string? GitHubUsername { get; set; }

    public bool Autorizado { get; set; }

    public bool EhOwner { get; set; }

    public PortalEqpMembroResponse? Membro { get; set; }
}

public class PortalEqpConviteResponse
{
    public string EqpId { get; set; } = string.Empty;

    public string AdmId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? ReservadoParaGitHub { get; set; }

    public string PapelEquipe { get; set; } = string.Empty;

    public string FluxoTrabalho { get; set; } = string.Empty;
}

public class PortalEqpMembroResponse
{
    public string EqpId { get; set; } = string.Empty;

    public string AdmId { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string GitHubUsername { get; set; } = string.Empty;

    public string PapelEquipe { get; set; } = string.Empty;

    public string FluxoTrabalho { get; set; } = string.Empty;

    public DateTime AtivadoEm { get; set; }
}

public class PortalEqpAtivarRequest
{
    [Required]
    [MaxLength(20)]
    public string EqpId { get; set; } = string.Empty;

    [Required]
    [MaxLength(160)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Senha { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string ConfirmarSenha { get; set; } = string.Empty;
}

public class PortalEqpRedefinirSenhaRequest
{
    [Required]
    [MinLength(8)]
    public string NovaSenha { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string ConfirmarSenha { get; set; } = string.Empty;
}

public class PortalEqpCriarConviteRequest
{
    [MaxLength(80)]
    public string? ReservadoParaGitHub { get; set; }

    [MaxLength(40)]
    public string PapelEquipe { get; set; } = "contributor";

    [MaxLength(40)]
    public string FluxoTrabalho { get; set; } = "fork_codespaces";
}

public class PortalEqpCriarLoteRequest : PortalEqpCriarConviteRequest
{
    [Range(1, 20)]
    public int Quantidade { get; set; } = 4;
}

public class PortalEqpAdminDbResponse
{
    public int Convites { get; set; }

    public int Membros { get; set; }

    public EquipeDbSettings Settings { get; set; } = new();

    public IReadOnlyCollection<PortalEqpConviteResponse> ConvitesResumo { get; set; } = [];

    public IReadOnlyCollection<PortalEqpMembroResponse> MembrosResumo { get; set; } = [];
}

public class SincronizarEquipeDbRequest
{
    public EquipeDbDocument? EquipeDb { get; set; }
}

public class SincronizarEquipeDbResponse
{
    public int MembrosImportados { get; set; }

    public int UsuariosCriados { get; set; }

    public int UsuariosAtualizados { get; set; }

    public int IdentificadoresCriados { get; set; }

    public int IdentificadoresAtualizados { get; set; }

    public string Mensagem { get; set; } = string.Empty;
}
