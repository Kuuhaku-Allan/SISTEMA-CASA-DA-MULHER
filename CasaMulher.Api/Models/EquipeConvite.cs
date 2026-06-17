namespace CasaMulher.Api.Models;

public class EquipeConvite
{
    public int Id { get; set; }

    public string CodigoEquipe { get; set; } = string.Empty;

    public string CodigoAtivacaoHash { get; set; } = string.Empty;

    public string Status { get; set; } = EquipeConviteStatus.Disponivel;

    public string? CriadoPorUserId { get; set; }

    public string? UsadoPorUserId { get; set; }

    public string? NomeInformado { get; set; }

    public string PapelEquipe { get; set; } = EquipePapeis.Contributor;

    public bool PrecisaFork { get; set; } = true;

    public bool UsaCodespaces { get; set; } = true;

    public string FluxoTrabalho { get; set; } = EquipeFluxosTrabalho.ForkCodespaces;

    public bool PodeCriarConvitesEquipe { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? UsadoEm { get; set; }

    public DateTime? RevogadoEm { get; set; }

    public string? Observacao { get; set; }
}

public static class EquipeConviteStatus
{
    public const string Disponivel = "Disponivel";
    public const string Usado = "Usado";
    public const string Revogado = "Revogado";

    public static readonly string[] Todos =
    [
        Disponivel,
        Usado,
        Revogado
    ];
}

public static class EquipePapeis
{
    public const string Owner = "owner";
    public const string Maintainer = "maintainer";
    public const string Contributor = "contributor";

    public static readonly string[] Todos =
    [
        Owner,
        Maintainer,
        Contributor
    ];

    public static bool PodeGerenciarConvites(string papel)
    {
        return string.Equals(papel, Owner, StringComparison.OrdinalIgnoreCase)
            || string.Equals(papel, Maintainer, StringComparison.OrdinalIgnoreCase);
    }
}
