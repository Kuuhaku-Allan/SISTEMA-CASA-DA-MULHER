namespace CasaMulher.Api.Models;

public class FuncionarioConvite
{
    public int Id { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;

    public string IdentificadorFuncionario { get; set; } = string.Empty;

    public string CodigoHash { get; set; } = string.Empty;

    public bool Usado { get; set; }

    public bool Cancelado { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime ExpiraEm { get; set; }

    public DateTime? UsadoEm { get; set; }

    public DateTime? CanceladoEm { get; set; }

    public string? UsuarioId { get; set; }
}
