using System.Security.Cryptography;

namespace CasaMulher.Api.Services;

public class SenhaTemporariaService : ISenhaTemporariaService
{
    public string Gerar()
    {
        var numero = RandomNumberGenerator.GetInt32(100000, 999999);
        return $"Temp@{numero}Aa";
    }
}
