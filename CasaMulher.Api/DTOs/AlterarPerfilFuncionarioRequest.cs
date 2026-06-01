using System.ComponentModel.DataAnnotations;

namespace CasaMulher.Api.DTOs;

public class AlterarPerfilFuncionarioRequest
{
    [Required]
    [MaxLength(40)]
    public string Perfil { get; set; } = string.Empty;
}
