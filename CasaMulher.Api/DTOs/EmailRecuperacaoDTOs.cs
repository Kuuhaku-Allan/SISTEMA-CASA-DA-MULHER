using System.ComponentModel.DataAnnotations;

namespace CasaMulher.Api.DTOs;

public class SolicitarEmailRecuperacaoRequest
{
    [Required]
    [EmailAddress]
    public string EmailRecuperacao { get; set; } = string.Empty;
}

public class ConfirmarEmailRecuperacaoRequest
{
    [Required]
    [EmailAddress]
    public string EmailRecuperacao { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;
}

public class EmailRecuperacaoResponse
{
    public string Mensagem { get; set; } = string.Empty;

    public string? EmailRecuperacao { get; set; }

    public bool EmailRecuperacaoConfirmado { get; set; }

    public DateTime? EmailRecuperacaoConfirmadoEm { get; set; }

    public string? StatusEmail { get; set; }

    public string? AvisoEmail { get; set; }

    public string? LinkConfirmacaoDesenvolvimento { get; set; }
}
