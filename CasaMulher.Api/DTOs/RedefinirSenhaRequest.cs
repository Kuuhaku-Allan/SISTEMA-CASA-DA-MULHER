using System.ComponentModel.DataAnnotations;

namespace CasaMulher.Api.DTOs;

public class RedefinirSenhaRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string NovaSenha { get; set; } = string.Empty;

    [Required]
    public string ConfirmarNovaSenha { get; set; } = string.Empty;
}
