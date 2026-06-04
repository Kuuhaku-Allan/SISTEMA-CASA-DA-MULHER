using System.Text.Json.Nodes;

namespace CasaMulher.Api.DTOs;

// ── Registro ───────────────────────────────────────────────────────────────

public sealed class PasskeyRegistrarIniciarResponse
{
    public string ChallengeId { get; set; } = string.Empty;

    /// <summary>PublicKeyCredentialCreationOptions serializado em JSON para o browser.</summary>
    public JsonNode? PublicKeyOptions { get; set; }
}

public sealed class PasskeyRegistrarConcluirRequest
{
    public string ChallengeId { get; set; } = string.Empty;

    public string NomeDispositivo { get; set; } = string.Empty;

    /// <summary>AuthenticatorAttestationRawResponse serializado em JSON pelo browser.</summary>
    public JsonNode? Credential { get; set; }
}

// ── Login ──────────────────────────────────────────────────────────────────

public sealed class PasskeyLoginIniciarResponse
{
    public string ChallengeId { get; set; } = string.Empty;

    /// <summary>PublicKeyCredentialRequestOptions serializado em JSON para o browser.</summary>
    public JsonNode? PublicKeyOptions { get; set; }
}

public sealed class PasskeyLoginConcluirRequest
{
    public string ChallengeId { get; set; } = string.Empty;

    /// <summary>AuthenticatorAssertionRawResponse serializado em JSON pelo browser.</summary>
    public JsonNode? Credential { get; set; }
}

public sealed class PasskeyLoginConcluirResponse
{
    /// <summary>JWT emitido — presente somente quando não exige reconfirmação.</summary>
    public string? Token { get; set; }

    public DateTime? ExpiraEm { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public string IdentificadorFuncionario { get; set; } = string.Empty;
    public bool DeveTrocarSenha { get; set; }
    public bool DoisFatoresObrigatorio { get; set; }
    public bool DoisFatoresAtivado { get; set; }

    /// <summary>Verdadeiro quando os 7 dias expiraram e é necessário reconfirmar.</summary>
    public bool RequerReconfirmacao { get; set; }

    /// <summary>Motivo da reconfirmação: primeiro_acesso ou prazo_7_dias.</summary>
    public string? MotivoReconfirmacao { get; set; }

    /// <summary>Token temporário (TTL 5min) para o endpoint reconfirmar. Presente somente quando RequerReconfirmacao = true.</summary>
    public string? ReconfirmacaoId { get; set; }

    /// <summary>Indica se o usuário tem 2FA ativo — necessário para saber se exibir o campo de código na tela de reconfirmação.</summary>
    public bool TemDoisFatores { get; set; }
}

// ── Reconfirmação dos 7 dias ───────────────────────────────────────────────

public sealed class PasskeyReconfirmarRequest
{
    /// <summary>Token gerado pelo endpoint login/concluir quando os 7 dias expiraram.</summary>
    public string ReconfirmacaoId { get; set; } = string.Empty;

    public string IdentificadorFuncionario { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;

    /// <summary>Necessário somente quando o usuário tem 2FA ativo.</summary>
    public string? CodigoDoAplicativo { get; set; }
}

// ── Lista de passkeys do usuário ───────────────────────────────────────────

public sealed class PasskeyListaItemResponse
{
    public int Id { get; set; }
    public string? NomeDispositivo { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? UltimoUsoEm { get; set; }
}
