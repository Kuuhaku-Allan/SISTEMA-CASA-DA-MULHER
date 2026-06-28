using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace CasaMulher.Api.Utils
{
    public class IdeContentSanitizerResult
    {
        public string Conteudo { get; }
        public int Removidos { get; }

        public IdeContentSanitizerResult(string conteudo, int removidos)
        {
            Conteudo = conteudo;
            Removidos = removidos;
        }
    }

    public static class IdeContentSanitizer
    {
        public static IdeContentSanitizerResult SanitizarArquivoIde(string? conteudo, string nomeArquivo, string usuarioId, ILogger logger, string modo)
        {
            if (string.IsNullOrEmpty(conteudo))
                return new IdeContentSanitizerResult(string.Empty, 0);

            var normalizado = conteudo
                .Normalize(NormalizationForm.FormC)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");

            var builder = new StringBuilder(normalizado.Length);
            var removidos = 0;

            foreach (var ch in normalizado)
            {
                var categoria = CharUnicodeInfo.GetUnicodeCategory(ch);

                var ehControlePermitido = ch == '\n' || ch == '\t';

                if (categoria == UnicodeCategory.Format)
                {
                    removidos++;
                    continue;
                }
                
                if (categoria == UnicodeCategory.NonSpacingMark ||
                    categoria == UnicodeCategory.SpacingCombiningMark ||
                    categoria == UnicodeCategory.EnclosingMark)
                {
                    removidos++;
                    continue;
                }

                if (char.IsControl(ch) && !ehControlePermitido)
                {
                    removidos++;
                    continue;
                }
                
                if (ch == '\u00A0')
                {
                    builder.Append(' ');
                    continue;
                }

                builder.Append(ch);
            }

            if (removidos > 0)
            {
                logger.LogWarning("IDE_CONTEUDO_SANITIZADO: Removidos {Quantidade} caracteres invisiveis no arquivo {Arquivo}. Modo: {Modo}", 
                    removidos, nomeArquivo, modo);
            }

            return new IdeContentSanitizerResult(builder.ToString(), removidos);
        }

        public static string SanitizarTextoCurtoIde(string? texto, string nomeCampo, string usuarioId, ILogger logger, string modo)
        {
            var resultado = SanitizarArquivoIde(texto, nomeCampo, usuarioId, logger, modo).Conteudo;
            return Regex.Replace(resultado, @"[ \t\r\n]+", " ").Trim();
        }

        public static string SanitizarEConverterParaBase64(string? conteudo, string nomeArquivo, string usuarioId, ILogger logger, string modo)
        {
            var sanitizadoResult = SanitizarArquivoIde(conteudo, nomeArquivo, usuarioId, logger, modo);
            var sanitizado = sanitizadoResult.Conteudo;
            
            var bytes = Encoding.UTF8.GetBytes(sanitizado);
            
            var contemCr = sanitizado.Contains('\r');
            var contemLf = sanitizado.Contains('\n');

            var base64 = Convert.ToBase64String(bytes);
            var roundTrip = Encoding.UTF8.GetString(Convert.FromBase64String(base64));

            logger.LogDebug(
                "IDE DEBUG PASSO 2 [Sanitizer] {Arquivo}: Antes CR={AntesCr}, Antes LF={AntesLf}, Depois CR={DepoisCr}, Depois LF={DepoisLf}",
                nomeArquivo,
                conteudo?.Count(c => c == '\r') ?? 0,
                conteudo?.Count(c => c == '\n') ?? 0,
                contemCr ? 1 : 0,
                sanitizado.Count(c => c == '\n')
            );

            logger.LogDebug(
                "IDE BASE64 {Arquivo}: Igual={Igual}, CR={CR}, LF={LF}",
                nomeArquivo,
                roundTrip == sanitizado,
                roundTrip.Count(c => c == '\r'),
                roundTrip.Count(c => c == '\n')
            );
            
            return base64;
        }
    }
}
