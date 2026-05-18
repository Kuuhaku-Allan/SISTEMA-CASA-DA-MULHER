namespace CasaMulher.Api.DTOs;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;

    public string NomeCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;
}
