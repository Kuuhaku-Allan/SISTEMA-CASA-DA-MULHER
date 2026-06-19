using System.Text.Json.Serialization;

namespace CasaMulher.Api.Services;

public class EquipeDbDocument
{
    public int SchemaVersion { get; set; } = 1;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public EquipeDbSettings Settings { get; set; } = new();

    public List<string> AllowlistGitHub { get; set; } = [];

    public List<EquipeDbConvite> Convites { get; set; } = [];

    public List<EquipeDbMembro> Membros { get; set; } = [];
}

public class EquipeDbSettings
{
    public string Org { get; set; } = "Sistema-Casa-da-Mulher";

    public string MainRepo { get; set; } = "SISTEMA-CASA-DA-MULHER";

    public string OwnerGitHub { get; set; } = "Kuuhaku-Allan";

    public string OwnerEqp { get; set; } = "EQP-000001";

    public string OwnerAdm { get; set; } = "ADM-000003";

    public int NextEqpNumber { get; set; } = 6;

    public int NextAdmNumber { get; set; } = 8;
}

public class EquipeDbConvite
{
    public string EqpId { get; set; } = string.Empty;

    public string AdmId { get; set; } = string.Empty;

    public string Status { get; set; } = EquipeDbStatusConvite.Disponivel;

    public string? ReservadoParaGitHub { get; set; }

    public string PapelEquipe { get; set; } = "contributor";

    public string FluxoTrabalho { get; set; } = "fork_codespaces";

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? UsadoEm { get; set; }
}

public class EquipeDbMembro
{
    public string EqpId { get; set; } = string.Empty;

    public string AdmId { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? EmailRecuperacao { get; set; }

    public bool EmailRecuperacaoConfirmado { get; set; }

    public string GitHubId { get; set; } = string.Empty;

    public string GitHubUsername { get; set; } = string.Empty;

    public string PapelEquipe { get; set; } = "contributor";

    public string FluxoTrabalho { get; set; } = "fork_codespaces";

    public string Status { get; set; } = "ativo";

    public string PasswordHash { get; set; } = string.Empty;

    public string SecurityStamp { get; set; } = string.Empty;

    // Mantido apenas para ler documentos antigos. ConcurrencyStamp pertence ao banco Identity local.
    public string ConcurrencyStamp { get; set; } = string.Empty;

    /// <summary>
    /// Data/hora da última atualização da senha no portal EQP.
    /// Usada para evitar sobrescrever senha local mais nova com hash antigo do JSON.
    /// </summary>
    public DateTime? SenhaAtualizadaEm { get; set; }

    /// <summary>
    /// Versão da senha no portal EQP. Incrementada a cada alteração de senha no portal.
    /// Usada para detectar atualizações no JSON.
    /// </summary>
    public int? PasswordVersion { get; set; }

    public DateTime AtivadoEm { get; set; } = DateTime.UtcNow;

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

public class EquipeDbEvent
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Tipo { get; set; } = string.Empty;

    public string EqpId { get; set; } = string.Empty;

    public string? AdmId { get; set; }

    public string? GitHubUsername { get; set; }

    public string? GitHubId { get; set; }

    public string Descricao { get; set; } = string.Empty;
}

public static class EquipeDbStatusConvite
{
    public const string Disponivel = "disponivel";
    public const string Reservado = "reservado";
    public const string Usado = "usado";
    public const string Revogado = "revogado";
}

public class EquipeDbFile
{
    public EquipeDbDocument Document { get; set; } = new();

    public string? Sha { get; set; }

    public bool Exists { get; set; }
}

public class EquipeDbGitHubException : Exception
{
    public int StatusCode { get; }

    public EquipeDbGitHubException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

internal sealed class GitHubContentResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("encoding")]
    public string Encoding { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;
}
