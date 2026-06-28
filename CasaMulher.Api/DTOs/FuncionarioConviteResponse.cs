namespace CasaMulher.Api.DTOs;

public class FuncionarioConviteResponse
{
    public int Id { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;

    public string? ProfessorCurso { get; set; }

    public string IdentificadorFuncionario { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; }

    public DateTime ExpiraEm { get; set; }

    public DateTime? UsadoEm { get; set; }

    public DateTime? CanceladoEm { get; set; }
}
