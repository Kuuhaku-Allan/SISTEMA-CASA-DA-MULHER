using System.ComponentModel.DataAnnotations;

namespace CasaMulher.Api.DTOs;

public class CriarFuncionarioConviteRequest
{
    [Required]
    [MaxLength(160)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Perfil { get; set; } = string.Empty;

    [Range(1, 90)]
    public int DiasParaExpirar { get; set; } = 7;
}
