using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace CasaMulher.Api.Services;

public static class HmlDbSnapshotCrypto
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("CMHML01");

    public static byte[] EncryptCompressed(byte[] database, byte[] key)
    {
        ValidateKey(key);
        using var compressedStream = new MemoryStream();
        using (var gzip = new GZipStream(compressedStream, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(database);
        }

        var compressed = compressedStream.ToArray();
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var ciphertext = new byte[compressed.Length];
        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, compressed, ciphertext, tag, Magic);

        using var result = new MemoryStream();
        result.Write(Magic);
        result.Write(nonce);
        result.Write(tag);
        result.Write(ciphertext);
        CryptographicOperations.ZeroMemory(compressed);
        return result.ToArray();
    }

    public static byte[] DecryptDecompressed(byte[] snapshot, byte[] key)
    {
        ValidateKey(key);
        var headerSize = Magic.Length + 12 + 16;
        if (snapshot.Length <= headerSize || !snapshot.AsSpan(0, Magic.Length).SequenceEqual(Magic))
        {
            throw new CryptographicException("Formato de snapshot inválido.");
        }

        var nonce = snapshot.AsSpan(Magic.Length, 12);
        var tag = snapshot.AsSpan(Magic.Length + 12, 16);
        var ciphertext = snapshot.AsSpan(headerSize);
        var compressed = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, tag.Length);
        aes.Decrypt(nonce, ciphertext, tag, compressed, Magic);

        try
        {
            using var input = new MemoryStream(compressed);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(compressed);
        }
    }

    public static byte[] ParseKey(string value)
    {
        try
        {
            var key = Convert.FromBase64String(value.Trim());
            ValidateKey(key);
            return key;
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("HML_DB_SNAPSHOT_KEY deve ser Base64 de 32 bytes.", ex);
        }
    }

    private static void ValidateKey(byte[] key)
    {
        if (key.Length != 32)
        {
            throw new InvalidOperationException("HML_DB_SNAPSHOT_KEY deve decodificar exatamente 32 bytes.");
        }
    }
}
