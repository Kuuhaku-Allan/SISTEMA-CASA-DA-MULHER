namespace CasaMulher.Api.DTOs;

public class DoisFatoresConfiguracaoResponse
{
    public string Mensagem { get; set; } = string.Empty;

    public string ChaveManual { get; set; } = string.Empty;

    public string AuthenticatorUri { get; set; } = string.Empty;

    public string QrCodeData { get; set; } = string.Empty;
}
