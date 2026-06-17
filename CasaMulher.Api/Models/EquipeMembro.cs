namespace CasaMulher.Api.Models;

public class EquipeMembro
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string CodigoEquipe { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string PapelEquipe { get; set; } = EquipePapeis.Contributor;

    public bool PrecisaFork { get; set; } = true;

    public bool UsaCodespaces { get; set; } = true;

    public string FluxoTrabalho { get; set; } = EquipeFluxosTrabalho.Desconhecido;

    public string? GitHubUsername { get; set; }

    public string? GitHubId { get; set; }

    public DateTime? GitHubVinculadoEm { get; set; }

    public string? ForkUrl { get; set; }

    public DateTime? UltimaVerificacaoGitHubEm { get; set; }

    public bool PodeCriarConvitesEquipe { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

public static class EquipeFluxosTrabalho
{
    public const string LocalOwner = "local_owner";
    public const string ForkCodespaces = "fork_codespaces";
    public const string PrecisaFork = "precisa_fork";
    public const string Desconhecido = "desconhecido";

    public static readonly string[] Todos =
    [
        LocalOwner,
        ForkCodespaces,
        PrecisaFork,
        Desconhecido
    ];
}
