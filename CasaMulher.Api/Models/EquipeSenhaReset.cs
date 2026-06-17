namespace CasaMulher.Api.Models;

public class EquipeSenhaReset
{
    public int Id { get; set; }

    public string CodigoEquipe { get; set; } = string.Empty;

    public string CodigoHash { get; set; } = string.Empty;

    public string GeradoPorUserId { get; set; } = string.Empty;

    public bool Usado { get; set; }

    public bool Revogado { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime ExpiraEm { get; set; }

    public DateTime? UsadoEm { get; set; }

    public DateTime? RevogadoEm { get; set; }
}
