using System.ComponentModel.DataAnnotations;

namespace CasaMulher.Api.DTOs;

public class CriarEquipeConviteRequest
{
    [MaxLength(500)]
    public string? Observacao { get; set; }

    [MaxLength(40)]
    public string PapelEquipe { get; set; } = "contributor";

    public bool PrecisaFork { get; set; } = true;

    public bool UsaCodespaces { get; set; } = true;

    [MaxLength(40)]
    public string FluxoTrabalho { get; set; } = "fork_codespaces";

    public bool PodeCriarConvitesEquipe { get; set; }
}

public class CriarEquipeConvitesLoteRequest : CriarEquipeConviteRequest
{
    [Range(1, 50)]
    public int Quantidade { get; set; } = 5;
}

public class BootstrapEquipeRequest
{
    [Range(1, 20)]
    public int QuantidadeIntegrantes { get; set; } = 5;

    public bool RegenerarCodigosDisponiveis { get; set; } = true;
}

public class BootstrapEquipeResponse
{
    public string Ambiente { get; set; } = string.Empty;

    public IReadOnlyCollection<BootstrapEquipeConviteResponse> Convites { get; set; } = [];
}

public class BootstrapEquipeConviteResponse
{
    public string CodigoEquipe { get; set; } = string.Empty;

    public string? CodigoAtivacao { get; set; }

    public string PapelEquipe { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Observacao { get; set; } = string.Empty;

    public bool Criado { get; set; }

    public bool Regenerado { get; set; }

    public bool Ativado { get; set; }
}

public class AtivarEquipeConviteRequest
{
    [Required]
    [MaxLength(20)]
    public string CodigoEquipe { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string CodigoAtivacao { get; set; } = string.Empty;

    [Required]
    [MaxLength(160)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Senha { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string ConfirmarSenha { get; set; } = string.Empty;
}

public class RedefinirSenhaEquipeRequest
{
    [Required]
    [MaxLength(20)]
    public string CodigoEquipe { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string CodigoRedefinicao { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NovaSenha { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string ConfirmarNovaSenha { get; set; } = string.Empty;
}

public class EquipeConviteResponse
{
    public int Id { get; set; }

    public string CodigoEquipe { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? NomeInformado { get; set; }

    public string PapelEquipe { get; set; } = string.Empty;

    public bool PrecisaFork { get; set; }

    public bool UsaCodespaces { get; set; }

    public string FluxoTrabalho { get; set; } = string.Empty;

    public bool PodeCriarConvitesEquipe { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime? UsadoEm { get; set; }

    public DateTime? RevogadoEm { get; set; }

    public string? Observacao { get; set; }
}

public class EquipeConviteCriadoResponse : EquipeConviteResponse
{
    public string CodigoAtivacao { get; set; } = string.Empty;
}

public class EquipeConvitesLoteResponse
{
    public IReadOnlyCollection<EquipeConviteCriadoResponse> Convites { get; set; } = [];
}

public class AtivarEquipeConviteResponse
{
    public string Mensagem { get; set; } = string.Empty;

    public string IdentificadorFuncionario { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;
}

public class EquipeMembroResponse
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string CodigoEquipe { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string PapelEquipe { get; set; } = string.Empty;

    public bool PrecisaFork { get; set; }

    public bool UsaCodespaces { get; set; }

    public string FluxoTrabalho { get; set; } = string.Empty;

    public string? GitHubUsername { get; set; }

    public string? GitHubId { get; set; }

    public DateTime? GitHubVinculadoEm { get; set; }

    public string? ForkUrl { get; set; }

    public DateTime? UltimaVerificacaoGitHubEm { get; set; }

    public bool PodeCriarConvitesEquipe { get; set; }

    public bool Ativo { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }

    public bool PodeEditar { get; set; }

    public bool PodeGerarResetSenha { get; set; }

    public bool EhVoce { get; set; }
}

public class AtualizarEquipeMembroRequest
{
    [MaxLength(40)]
    public string PapelEquipe { get; set; } = string.Empty;

    public bool PrecisaFork { get; set; }

    public bool UsaCodespaces { get; set; }

    [MaxLength(40)]
    public string FluxoTrabalho { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? GitHubUsername { get; set; }

    [MaxLength(80)]
    public string? GitHubId { get; set; }

    [MaxLength(300)]
    public string? ForkUrl { get; set; }

    public bool PodeCriarConvitesEquipe { get; set; }

    public bool Ativo { get; set; } = true;
}

public class GerarRedefinicaoSenhaEquipeResponse
{
    public string CodigoEquipe { get; set; } = string.Empty;

    public string CodigoRedefinicao { get; set; } = string.Empty;

    public DateTime ExpiraEm { get; set; }
}

public class EquipeGithubStatusResponse
{
    public bool OAuthConfigurado { get; set; }

    public string Organization { get; set; } = string.Empty;

    public string Repository { get; set; } = string.Empty;

    public string OwnerUsername { get; set; } = string.Empty;

    public string Mensagem { get; set; } = string.Empty;
}

public class EquipeGithubAtividadeResponse
{
    public bool Disponivel { get; set; }

    public string Mensagem { get; set; } = string.Empty;

    public DateTime? AtualizadoEm { get; set; }

    public IReadOnlyCollection<EquipeGithubPullRequestResponse> PullRequests { get; set; } = [];
}

public class EquipeGithubPullRequestResponse
{
    public int Numero { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string Autor { get; set; } = string.Empty;

    public string Branch { get; set; } = string.Empty;

    public bool VeioDeFork { get; set; }

    public DateTime? CriadoEm { get; set; }

    public DateTime? FechadoEm { get; set; }

    public DateTime? MergeadoEm { get; set; }

    public string Url { get; set; } = string.Empty;
}
