using System.ComponentModel.DataAnnotations;

namespace CasaMulher.Api.DTOs;

public class TrocarSenhaObrigatoriaRequest
{
    [Required]
    public string SenhaAtual { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NovaSenha { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string ConfirmarNovaSenha { get; set; } = string.Empty;
}
