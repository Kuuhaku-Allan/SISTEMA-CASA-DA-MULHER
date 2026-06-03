namespace CasaMulher.Api.DTOs;

public class CriarFuncionarioConviteResponse
{
    public int Id { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;

    public string IdentificadorFuncionario { get; set; } = string.Empty;

    public string CodigoCadastro { get; set; } = string.Empty;

    public string LinkCadastro { get; set; } = string.Empty;

    public DateTime ExpiraEm { get; set; }

    public bool EmailEnviado { get; set; }

    public string? StatusEmail { get; set; }

    public string? AvisoEmail { get; set; }

    public string? AvisoEmailAlias { get; set; }
}
