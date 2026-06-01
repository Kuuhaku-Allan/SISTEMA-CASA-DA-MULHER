using System.Security.Cryptography;
using System.Text;

namespace CasaMulher.Api.Services;

public class ConviteCodigoService : IConviteCodigoService
{
    private const string CodigoCaracteres = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private readonly byte[] _hashSecret;

    public ConviteCodigoService(IConfiguration configuration)
    {
        var secret = configuration["Convites:HashSecret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Configure Convites:HashSecret para proteger os hashes dos códigos de convite.");
        }

        _hashSecret = Encoding.UTF8.GetBytes(secret);
    }

    public string GerarCodigoCadastro()
    {
        Span<char> bloco = stackalloc char[4];

        for (var index = 0; index < bloco.Length; index++)
        {
            var caractereIndex = RandomNumberGenerator.GetInt32(CodigoCaracteres.Length);
            bloco[index] = CodigoCaracteres[caractereIndex];
        }

        return $"CM-{new string(bloco)}-{DateTime.UtcNow.Year}";
    }

    public string GerarHash(string codigo)
    {
        var codigoNormalizado = NormalizarCodigo(codigo);
        var codigoBytes = Encoding.UTF8.GetBytes(codigoNormalizado);
        var hashBytes = HMACSHA256.HashData(_hashSecret, codigoBytes);

        return Convert.ToHexString(hashBytes);
    }

    public bool CodigoCorresponde(string codigo, string hashSalvo)
    {
        if (string.IsNullOrWhiteSpace(hashSalvo))
        {
            return false;
        }

        var hashCalculado = GerarHash(codigo);
        var hashCalculadoBytes = Encoding.UTF8.GetBytes(hashCalculado);
        var hashSalvoBytes = Encoding.UTF8.GetBytes(hashSalvo);

        return hashCalculadoBytes.Length == hashSalvoBytes.Length
            && CryptographicOperations.FixedTimeEquals(hashCalculadoBytes, hashSalvoBytes);
    }

    private static string NormalizarCodigo(string codigo)
    {
        return codigo.Trim().ToUpperInvariant();
    }
}
