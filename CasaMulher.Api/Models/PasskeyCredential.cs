namespace CasaMulher.Api.Models;

public class PasskeyCredential
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    /// <summary>Identificador único da credencial no autenticador (rawId do browser).</summary>
    public byte[] CredentialId { get; set; } = Array.Empty<byte>();

    /// <summary>Chave pública COSE armazenada para verificar assinaturas futuras.</summary>
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();

    /// <summary>Contador de assinaturas para detecção de clone/replay.</summary>
    public uint SignatureCounter { get; set; }

    /// <summary>Nome amigável informado pelo usuário no momento do cadastro (ex: "iPhone de Maria").</summary>
    public string? NomeDispositivo { get; set; }

    /// <summary>JSON com lista de transportes do autenticador (usb, nfc, ble, internal, hybrid).</summary>
    public string? Transports { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? UltimoUsoEm { get; set; }
}
