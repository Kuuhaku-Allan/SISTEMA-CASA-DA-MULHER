namespace CasaMulher.Api.Models;

public class UserLoginIdentifier
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Identificador { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}
