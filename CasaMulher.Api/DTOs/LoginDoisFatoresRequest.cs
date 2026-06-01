using System.ComponentModel.DataAnnotations;

namespace CasaMulher.Api.DTOs;

public class LoginDoisFatoresRequest
{
    [Required]
    public string LoginTemporario { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(16)]
    public string Codigo { get; set; } = string.Empty;
}
