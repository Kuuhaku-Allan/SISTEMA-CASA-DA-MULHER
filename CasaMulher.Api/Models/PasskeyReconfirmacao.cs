namespace CasaMulher.Api.Models;

public class PasskeyReconfirmacao
{
    public int Id { get; set; }

    /// <summary>GUID gerado após validar a assinatura da passkey quando os 7 dias expiraram.</summary>
    public string ReconfirmacaoId { get; set; } = string.Empty;

    /// <summary>Usuário identificado após verificar a assinatura com sucesso.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>CredentialId da passkey usada no login (para auditoria).</summary>
    public byte[] CredentialId { get; set; } = Array.Empty<byte>();

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    /// <summary>TTL de 5 minutos — expira após a janela de reconfirmação.</summary>
    public DateTime ExpiracaoEm { get; set; }
}
