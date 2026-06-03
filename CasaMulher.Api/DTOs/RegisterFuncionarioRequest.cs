using System.ComponentModel.DataAnnotations;

namespace CasaMulher.Api.DTOs;

public class RegisterFuncionarioRequest
{
    [MaxLength(160)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Senha { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string ConfirmarSenha { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string CodigoCadastro { get; set; } = string.Empty;
}
