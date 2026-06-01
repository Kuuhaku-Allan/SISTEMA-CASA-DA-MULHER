namespace CasaMulher.Api.DTOs;

public class AuditoriaEventoResponse
{
    public int Id { get; set; }

    public string UsuarioId { get; set; } = string.Empty;

    public string IdentificadorFuncionario { get; set; } = string.Empty;

    public string NomeFuncionario { get; set; } = string.Empty;

    public string PerfilFuncionario { get; set; } = string.Empty;

    public string Acao { get; set; } = string.Empty;

    public string Entidade { get; set; } = string.Empty;

    public string? EntidadeId { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public string? IpOrigem { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CriadoEm { get; set; }
}
