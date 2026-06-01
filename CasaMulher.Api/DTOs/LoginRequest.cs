using System.ComponentModel.DataAnnotations;

namespace CasaMulher.Api.DTOs;

public class LoginRequest
{
    [MaxLength(256)]
    public string Identificador { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Senha { get; set; } = string.Empty;
}
