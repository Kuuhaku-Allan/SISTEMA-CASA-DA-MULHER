using CasaMulher.Api.Data;
using CasaMulher.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Services;

public class GeradorIdentificadorFuncionarioService : IFuncionarioIdentificadorService
{
    private readonly AppDbContext _dbContext;

    public GeradorIdentificadorFuncionarioService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GerarProximoAsync(string perfil)
    {
        var prefixo = ObterPrefixo(perfil);
        var prefixoBusca = $"{prefixo}-";
        var identificadoresUsuarios = await _dbContext.Users
            .Where(usuario => usuario.IdentificadorFuncionario.StartsWith(prefixoBusca))
            .Select(usuario => usuario.IdentificadorFuncionario)
            .ToListAsync();
        var identificadoresConvites = await _dbContext.FuncionariosConvites
            .Where(convite => convite.IdentificadorFuncionario.StartsWith(prefixoBusca))
            .Select(convite => convite.IdentificadorFuncionario)
            .ToListAsync();
        var identificadores = identificadoresUsuarios.Concat(identificadoresConvites);

        var proximoNumero = identificadores
            .Select(identificador => ExtrairNumero(prefixoBusca, identificador))
            .DefaultIfEmpty(0)
            .Max() + 1;

        for (var tentativa = 0; tentativa < 100; tentativa++)
        {
            var identificador = $"{prefixo}-{proximoNumero + tentativa:000000}";
            var existeUsuario = await _dbContext.Users.AnyAsync(usuario =>
                usuario.IdentificadorFuncionario == identificador
                || usuario.NormalizedUserName == identificador);
            var existeConvite = await _dbContext.FuncionariosConvites.AnyAsync(convite =>
                convite.IdentificadorFuncionario == identificador);

            if (!existeUsuario && !existeConvite)
            {
                return identificador;
            }
        }

        throw new InvalidOperationException("Não foi possível gerar identificador único para o funcionário.");
    }

    private static string ObterPrefixo(string perfil)
    {
        return perfil.Trim().ToLowerInvariant() switch
        {
            PerfisAcesso.Adm => "ADM",
            PerfisAcesso.Recepcao => "REC",
            PerfisAcesso.Professor => "PRO",
            PerfisAcesso.AssistenteSocial => "SOC",
            PerfisAcesso.Juridico => "JUR",
            _ => throw new ArgumentException("Perfil inválido para gerar identificador.", nameof(perfil))
        };
    }

    private static int ExtrairNumero(string prefixoBusca, string identificador)
    {
        if (string.IsNullOrWhiteSpace(identificador)
            || !identificador.StartsWith(prefixoBusca, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return int.TryParse(identificador[prefixoBusca.Length..], out var numero) ? numero : 0;
    }
}
