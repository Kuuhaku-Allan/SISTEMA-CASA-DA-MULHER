namespace CasaMulher.Api.DTOs;

public class EmailEventoResponse
{
    public int Id { get; set; }

    public string Destinatario { get; set; } = string.Empty;

    public string Assunto { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Erro { get; set; }

    public DateTime CriadoEm { get; set; }
}
