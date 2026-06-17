using CasaMulher.Api.Data;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Services;

public class EquipeDbSyncService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IEquipeDbGitHubService _githubDbService;

    public EquipeDbSyncService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IEquipeDbGitHubService githubDbService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _githubDbService = githubDbService;
    }

    public async Task<SincronizarEquipeDbResponse> SincronizarAsync(
        EquipeDbDocument? document,
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            document = (await _githubDbService.LerAsync(cancellationToken)).Document;
        }

        EquipeDbGitHubService.NormalizarDocumento(document);

        var response = new SincronizarEquipeDbResponse();

        await GarantirRoleAsync(PerfisAcesso.Equipe);
        await GarantirRoleAsync(PerfisAcesso.Adm);

        foreach (var membro in document.Membros.Where(MembroAtivo))
        {
            var usuario = await EncontrarUsuarioPorMembroAsync(membro, cancellationToken);
            var criouUsuario = false;

            if (usuario is null)
            {
                usuario = CriarUsuario(membro);
                var createResult = await _userManager.CreateAsync(usuario);

                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Não foi possível criar usuário para {membro.EqpId}: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
                }

                criouUsuario = true;
                response.UsuariosCriados++;
            }
            else
            {
                AtualizarUsuario(usuario, membro);
                var updateResult = await _userManager.UpdateAsync(usuario);

                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Não foi possível atualizar usuário para {membro.EqpId}: {string.Join("; ", updateResult.Errors.Select(e => e.Description))}");
                }

                response.UsuariosAtualizados++;
            }

            await GarantirRoleUsuarioAsync(usuario, PerfisAcesso.Equipe);

            if (membro.AdmId.StartsWith("ADM-", StringComparison.OrdinalIgnoreCase))
            {
                await GarantirRoleUsuarioAsync(usuario, PerfisAcesso.Adm);
            }

            response.IdentificadoresCriados += await GarantirIdentificadorAsync(usuario.Id, membro.EqpId, "EQP", cancellationToken) ? 1 : 0;
            response.IdentificadoresCriados += await GarantirIdentificadorAsync(usuario.Id, membro.AdmId, "ADM", cancellationToken) ? 1 : 0;

            await SincronizarEquipeMembroAsync(usuario, membro, cancellationToken);
            response.MembrosImportados++;

            if (!criouUsuario)
            {
                response.IdentificadoresAtualizados += await AtualizarIdentificadoresExistentesAsync(usuario.Id, membro, cancellationToken);
            }
        }

        response.Mensagem = $"Sincronização concluída com {response.MembrosImportados} membro(s).";
        return response;
    }

    private async Task<ApplicationUser?> EncontrarUsuarioPorMembroAsync(
        EquipeDbMembro membro,
        CancellationToken cancellationToken)
    {
        var identificadores = new[] { membro.EqpId, membro.AdmId }
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim().ToUpperInvariant())
            .ToArray();

        var alias = await _dbContext.UserLoginIdentifiers
            .Where(item => item.Ativo && identificadores.Contains(item.Identificador.ToUpper()))
            .OrderBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (alias is not null)
        {
            return await _userManager.FindByIdAsync(alias.UserId);
        }

        return await _dbContext.Users
            .Where(usuario =>
                identificadores.Contains(usuario.IdentificadorFuncionario.ToUpper())
                || identificadores.Contains(usuario.NormalizedUserName ?? string.Empty))
            .OrderBy(usuario => usuario.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static ApplicationUser CriarUsuario(EquipeDbMembro membro)
    {
        var identificadorPrincipal = string.IsNullOrWhiteSpace(membro.EqpId)
            ? membro.AdmId.Trim().ToUpperInvariant()
            : membro.EqpId.Trim().ToUpperInvariant();

        return new ApplicationUser
        {
            NomeCompleto = membro.Nome.Trim(),
            Email = $"{identificadorPrincipal.ToLowerInvariant()}@equipe.local",
            UserName = identificadorPrincipal,
            NormalizedUserName = identificadorPrincipal,
            IdentificadorFuncionario = identificadorPrincipal,
            Perfil = PerfisAcesso.Equipe,
            EmailConfirmed = true,
            Ativo = true,
            DoisFatoresObrigatorio = false,
            PasswordHash = membro.PasswordHash,
            SecurityStamp = string.IsNullOrWhiteSpace(membro.SecurityStamp) ? Guid.NewGuid().ToString("N") : membro.SecurityStamp,
            ConcurrencyStamp = string.IsNullOrWhiteSpace(membro.ConcurrencyStamp) ? Guid.NewGuid().ToString("N") : membro.ConcurrencyStamp,
            CriadoEm = membro.AtivadoEm == default ? DateTime.UtcNow : membro.AtivadoEm
        };
    }

    private static void AtualizarUsuario(ApplicationUser usuario, EquipeDbMembro membro)
    {
        usuario.NomeCompleto = membro.Nome.Trim();
        usuario.Ativo = true;
        usuario.PasswordHash = membro.PasswordHash;
        usuario.SecurityStamp = string.IsNullOrWhiteSpace(membro.SecurityStamp) ? Guid.NewGuid().ToString("N") : membro.SecurityStamp;
        usuario.ConcurrencyStamp = string.IsNullOrWhiteSpace(membro.ConcurrencyStamp) ? Guid.NewGuid().ToString("N") : membro.ConcurrencyStamp;

        if (string.IsNullOrWhiteSpace(usuario.Perfil))
        {
            usuario.Perfil = PerfisAcesso.Equipe;
        }
    }

    private async Task<bool> GarantirIdentificadorAsync(
        string userId,
        string identificador,
        string tipo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identificador))
        {
            return false;
        }

        var normalizado = identificador.Trim().ToUpperInvariant();
        var existente = await _dbContext.UserLoginIdentifiers
            .SingleOrDefaultAsync(item => item.Identificador == normalizado, cancellationToken);

        if (existente is null)
        {
            _dbContext.UserLoginIdentifiers.Add(new UserLoginIdentifier
            {
                UserId = userId,
                Identificador = normalizado,
                Tipo = tipo,
                Ativo = true,
                CriadoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        existente.UserId = userId;
        existente.Tipo = tipo;
        existente.Ativo = true;
        existente.AtualizadoEm = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return false;
    }

    private async Task<int> AtualizarIdentificadoresExistentesAsync(
        string userId,
        EquipeDbMembro membro,
        CancellationToken cancellationToken)
    {
        var ids = new[] { membro.EqpId, membro.AdmId }
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim().ToUpperInvariant())
            .ToArray();

        return await _dbContext.UserLoginIdentifiers
            .Where(item => ids.Contains(item.Identificador) && item.UserId == userId && item.Ativo)
            .CountAsync(cancellationToken);
    }

    private async Task SincronizarEquipeMembroAsync(
        ApplicationUser usuario,
        EquipeDbMembro membro,
        CancellationToken cancellationToken)
    {
        var equipeMembro = await _dbContext.EquipeMembros
            .SingleOrDefaultAsync(item => item.UserId == usuario.Id || item.CodigoEquipe == membro.EqpId, cancellationToken);

        if (equipeMembro is null)
        {
            _dbContext.EquipeMembros.Add(new EquipeMembro
            {
                UserId = usuario.Id,
                CodigoEquipe = membro.EqpId,
                Nome = membro.Nome,
                PapelEquipe = membro.PapelEquipe,
                PrecisaFork = !string.Equals(membro.FluxoTrabalho, "local_owner", StringComparison.OrdinalIgnoreCase),
                UsaCodespaces = string.Equals(membro.FluxoTrabalho, "fork_codespaces", StringComparison.OrdinalIgnoreCase),
                FluxoTrabalho = membro.FluxoTrabalho,
                GitHubUsername = membro.GitHubUsername,
                GitHubId = membro.GitHubId,
                GitHubVinculadoEm = membro.AtivadoEm,
                PodeCriarConvitesEquipe = string.Equals(membro.PapelEquipe, "owner", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(membro.PapelEquipe, "maintainer", StringComparison.OrdinalIgnoreCase),
                Ativo = true,
                CriadoEm = membro.AtivadoEm,
                AtualizadoEm = membro.AtualizadoEm
            });
        }
        else
        {
            equipeMembro.UserId = usuario.Id;
            equipeMembro.CodigoEquipe = membro.EqpId;
            equipeMembro.Nome = membro.Nome;
            equipeMembro.PapelEquipe = membro.PapelEquipe;
            equipeMembro.FluxoTrabalho = membro.FluxoTrabalho;
            equipeMembro.PrecisaFork = !string.Equals(membro.FluxoTrabalho, "local_owner", StringComparison.OrdinalIgnoreCase);
            equipeMembro.UsaCodespaces = string.Equals(membro.FluxoTrabalho, "fork_codespaces", StringComparison.OrdinalIgnoreCase);
            equipeMembro.GitHubUsername = membro.GitHubUsername;
            equipeMembro.GitHubId = membro.GitHubId;
            equipeMembro.GitHubVinculadoEm ??= membro.AtivadoEm;
            equipeMembro.PodeCriarConvitesEquipe = string.Equals(membro.PapelEquipe, "owner", StringComparison.OrdinalIgnoreCase)
                || string.Equals(membro.PapelEquipe, "maintainer", StringComparison.OrdinalIgnoreCase);
            equipeMembro.Ativo = true;
            equipeMembro.AtualizadoEm = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task GarantirRoleAsync(string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private async Task GarantirRoleUsuarioAsync(ApplicationUser usuario, string role)
    {
        if (!await _userManager.IsInRoleAsync(usuario, role))
        {
            await _userManager.AddToRoleAsync(usuario, role);
        }
    }

    private static bool MembroAtivo(EquipeDbMembro membro)
    {
        return string.Equals(membro.Status, "ativo", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(membro.EqpId)
            && !string.IsNullOrWhiteSpace(membro.AdmId)
            && !string.IsNullOrWhiteSpace(membro.PasswordHash);
    }
}
