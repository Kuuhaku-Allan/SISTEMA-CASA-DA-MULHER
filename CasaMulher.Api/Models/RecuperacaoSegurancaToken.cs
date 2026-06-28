namespace CasaMulher.Api.Models;

public class RecuperacaoSegurancaToken
{
    public int Id { get; set; }

    public string FuncionarioId { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public string Tipo { get; set; } = "RecuperacaoSeguranca";

    public string EmailDestino { get; set; } = string.Empty;

    public DateTime ExpiraEm { get; set; }

    public DateTime? UsadoEm { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public string IpSolicitante { get; set; } = string.Empty;

    public string UserAgent { get; set; } = string.Empty;

    public int Tentativas { get; set; }
}
