namespace CasaMulher.Api.DTOs;

public class ConvitePublicoResponse
{
    public string NomeCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;

    public string? ProfessorCurso { get; set; }

    public string IdentificadorFuncionario { get; set; } = string.Empty;

    public DateTime ExpiraEm { get; set; }
}
