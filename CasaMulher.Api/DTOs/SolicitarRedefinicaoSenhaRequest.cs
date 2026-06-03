using System.ComponentModel.DataAnnotations;

namespace CasaMulher.Api.DTOs;

public class SolicitarRedefinicaoSenhaRequest
{
    [MaxLength(64)]
    public string IdentificadorFuncionario { get; set; } = string.Empty;
}
