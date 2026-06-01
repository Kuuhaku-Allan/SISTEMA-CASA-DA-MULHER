using System.ComponentModel.DataAnnotations;

namespace CasaMulher.Api.DTOs;

public class ConfirmarDoisFatoresRequest
{
    [Required]
    [MinLength(6)]
    [MaxLength(16)]
    public string Codigo { get; set; } = string.Empty;
}
