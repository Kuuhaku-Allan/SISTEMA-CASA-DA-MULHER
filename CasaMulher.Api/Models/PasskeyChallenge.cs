namespace CasaMulher.Api.Models;

public class PasskeyChallenge
{
    public int Id { get; set; }

    /// <summary>GUID gerado pelo servidor para identificar este challenge.</summary>
    public string ChallengeId { get; set; } = string.Empty;

    /// <summary>Bytes aleatórios do challenge enviados ao browser.</summary>
    public byte[] ChallengeBytes { get; set; } = Array.Empty<byte>();

    /// <summary>"Registro" ou "Login".</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>
    /// JSON completo das options geradas pelo Fido2NetLib na fase iniciar.
    /// Necessário para validar a resposta do browser na fase concluir com o mesmo contexto.
    /// </summary>
    public string OptionsJson { get; set; } = string.Empty;

    /// <summary>Preenchido para Registro; nulo para Login (usuário ainda desconhecido).</summary>
    public string? UserId { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime ExpiracaoEm { get; set; }
}
