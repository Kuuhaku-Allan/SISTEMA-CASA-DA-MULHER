using CasaMulher.Api.Data;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

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
                // Atualização seletiva: não usar UserManager.UpdateAsync porque ele persiste
                // o objeto inteiro e pode sobrescrever campos de segurança via tracking do EF.
                // Guardamos os valores sensíveis antes e marcamos explicitamente só o permitido.
                var twoFactorAntes = usuario.TwoFactorEnabled;
                var securityStampAntes = usuario.SecurityStamp;
                var passwordHashAntes = usuario.PasswordHash;
                var passkeyReconfAntes = usuario.PasskeyReconfirmadoEm;

                AtualizarUsuario(usuario, membro);

                var entry = _dbContext.Entry(usuario);
                // Campos que o sync EQP pode atualizar
                entry.Property(u => u.NomeCompleto).IsModified = true;
                entry.Property(u => u.Ativo).IsModified = true;
                entry.Property(u => u.Perfil).IsModified = true;
                entry.Property(u => u.Email).IsModified = true;
                entry.Property(u => u.NormalizedEmail).IsModified = true;
                entry.Property(u => u.EmailRecuperacao).IsModified = true;
                entry.Property(u => u.EmailRecuperacaoConfirmado).IsModified = true;
                entry.Property(u => u.EmailRecuperacaoConfirmadoEm).IsModified = true;
                entry.Property(u => u.EquipeDbPasswordUpdatedAt).IsModified = true;
                entry.Property(u => u.EquipeDbPasswordVersion).IsModified = true;

                // Senha e SecurityStamp: só se AtualizarUsuario() realmente mudou
                if (!string.Equals(usuario.PasswordHash, passwordHashAntes, StringComparison.Ordinal))
                {
                    entry.Property(u => u.PasswordHash).IsModified = true;
                    entry.Property(u => u.SecurityStamp).IsModified = true;
                }
                else
                {
                    entry.Property(u => u.PasswordHash).IsModified = false;
                    entry.Property(u => u.SecurityStamp).IsModified = false;
                }

                // Campos de segurança que o sync NUNCA pode alterar
                entry.Property(u => u.TwoFactorEnabled).IsModified = false;
                entry.Property(u => u.PasskeyReconfirmadoEm).IsModified = false;
                entry.Property(u => u.LockoutEnabled).IsModified = false;
                entry.Property(u => u.LockoutEnd).IsModified = false;
                entry.Property(u => u.AccessFailedCount).IsModified = false;
                entry.Property(u => u.ConcurrencyStamp).IsModified = false;

                await _dbContext.SaveChangesAsync(cancellationToken);

                // Validação pós-sync: confirmar que campos de segurança críticos não mudaram
                if (usuario.TwoFactorEnabled != twoFactorAntes
                    || usuario.PasskeyReconfirmadoEm != passkeyReconfAntes)
                {
                    throw new InvalidOperationException(
                        $"Sync EQP alterou campo de segurança indevidamente para {membro.EqpId}. " +
                        $"TwoFactor: {twoFactorAntes}->{usuario.TwoFactorEnabled}. " +
                        $"PasskeyReconf: {passkeyReconfAntes}->{usuario.PasskeyReconfirmadoEm}. Abortando.");
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

        var aliasUserIds = await _dbContext.UserLoginIdentifiers
            .Where(item => item.Ativo && identificadores.Contains(item.Identificador.ToUpper()))
            .OrderBy(item => item.Id)
            .Select(item => item.UserId)
            .Distinct()
            .Take(2)
            .ToListAsync(cancellationToken);

        if (aliasUserIds.Count > 1)
        {
            throw new InvalidOperationException(
                $"Os aliases de {membro.EqpId}/{membro.AdmId} apontam para usuários diferentes. Execute a auditoria de segurança antes de sincronizar.");
        }

        if (aliasUserIds.Count == 1)
        {
            return await _userManager.FindByIdAsync(aliasUserIds[0]);
        }

        var usuarios = await _dbContext.Users
            .Where(usuario =>
                identificadores.Contains(usuario.IdentificadorFuncionario.ToUpper())
                || identificadores.Contains(usuario.NormalizedUserName ?? string.Empty))
            .OrderBy(usuario => usuario.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (usuarios.Count > 1)
        {
            throw new InvalidOperationException(
                $"Foram encontradas contas diferentes para {membro.EqpId}/{membro.AdmId}. Execute a auditoria de segurança antes de sincronizar.");
        }

        return usuarios.SingleOrDefault();
    }

    private static ApplicationUser CriarUsuario(EquipeDbMembro membro)
    {
        var identificadorPrincipal = string.IsNullOrWhiteSpace(membro.EqpId)
            ? membro.AdmId.Trim().ToUpperInvariant()
            : membro.EqpId.Trim().ToUpperInvariant();
        var emailPrincipal = ObterEmailReal(membro.Email, membro.EmailRecuperacao)
            ?? CriarEmailPlaceholder(identificadorPrincipal);
        var emailRecuperacao = ObterEmailReal(membro.EmailRecuperacao, membro.Email);
        var recuperacaoConfirmada = emailRecuperacao is not null
            && EhEmailReal(membro.EmailRecuperacao)
            && membro.EmailRecuperacaoConfirmado;

        return new ApplicationUser
        {
            NomeCompleto = membro.Nome.Trim(),
            Email = emailPrincipal,
            EmailConfirmed = recuperacaoConfirmada
                && string.Equals(emailPrincipal, emailRecuperacao, StringComparison.OrdinalIgnoreCase),
            EmailRecuperacao = emailRecuperacao,
            EmailRecuperacaoConfirmado = recuperacaoConfirmada,
            EmailRecuperacaoConfirmadoEm = recuperacaoConfirmada
                ? (membro.AtualizadoEm == default ? DateTime.UtcNow : membro.AtualizadoEm)
                : null,
            UserName = identificadorPrincipal,
            NormalizedUserName = identificadorPrincipal,
            IdentificadorFuncionario = identificadorPrincipal,
            Perfil = PerfisAcesso.Equipe,
            Ativo = true,
            DoisFatoresObrigatorio = false,
            PasswordHash = membro.PasswordHash,
            SecurityStamp = string.IsNullOrWhiteSpace(membro.SecurityStamp) ? Guid.NewGuid().ToString("N") : membro.SecurityStamp,
            EquipeDbPasswordUpdatedAt = membro.SenhaAtualizadaEm ?? DateTime.UtcNow,
            EquipeDbPasswordVersion = membro.PasswordVersion ?? 1,
            CriadoEm = membro.AtivadoEm == default ? DateTime.UtcNow : membro.AtivadoEm
        };
    }

    private static void AtualizarUsuario(ApplicationUser usuario, EquipeDbMembro membro)
    {
        usuario.NomeCompleto = membro.Nome.Trim();
        usuario.Ativo = true;
        PreencherEmailsAusentes(usuario, membro);

        var versaoRemota = membro.PasswordVersion ?? 0;
        var senhaRemotaAtualizadaEm = membro.SenhaAtualizadaEm;
        var possuiVersaoRemota = versaoRemota > 0 || senhaRemotaAtualizadaEm.HasValue;
        var deveAtualizarSenha = possuiVersaoRemota
            && (
                versaoRemota > usuario.EquipeDbPasswordVersion
                || (
                    senhaRemotaAtualizadaEm.HasValue
                    && (
                        !usuario.EquipeDbPasswordUpdatedAt.HasValue
                        || senhaRemotaAtualizadaEm.Value > usuario.EquipeDbPasswordUpdatedAt.Value
                    )
                )
            );

        if (deveAtualizarSenha && !string.Equals(usuario.PasswordHash, membro.PasswordHash, StringComparison.Ordinal))
        {
            usuario.PasswordHash = membro.PasswordHash;
            usuario.SecurityStamp = string.IsNullOrWhiteSpace(membro.SecurityStamp)
                ? Guid.NewGuid().ToString("N")
                : membro.SecurityStamp;
            usuario.EquipeDbPasswordUpdatedAt = senhaRemotaAtualizadaEm ?? DateTime.UtcNow;
            usuario.EquipeDbPasswordVersion = Math.Max(versaoRemota, usuario.EquipeDbPasswordVersion + 1);
        }
        else if (possuiVersaoRemota && string.Equals(usuario.PasswordHash, membro.PasswordHash, StringComparison.Ordinal))
        {
            usuario.EquipeDbPasswordUpdatedAt = senhaRemotaAtualizadaEm ?? usuario.EquipeDbPasswordUpdatedAt;
            usuario.EquipeDbPasswordVersion = Math.Max(usuario.EquipeDbPasswordVersion, versaoRemota);
        }

        usuario.Perfil = usuario.IdentificadorFuncionario.StartsWith("ADM-", StringComparison.OrdinalIgnoreCase)
            ? PerfisAcesso.Adm
            : PerfisAcesso.Equipe;
    }

    private static void PreencherEmailsAusentes(ApplicationUser usuario, EquipeDbMembro membro)
    {
        var emailPrincipalRemoto = ObterEmailReal(membro.Email, membro.EmailRecuperacao);

        if (!EhEmailReal(usuario.Email) && emailPrincipalRemoto is not null)
        {
            usuario.Email = emailPrincipalRemoto;
            usuario.EmailConfirmed = membro.EmailRecuperacaoConfirmado
                && EhEmailReal(membro.EmailRecuperacao)
                && string.Equals(emailPrincipalRemoto, membro.EmailRecuperacao, StringComparison.OrdinalIgnoreCase);
        }

        var emailRecuperacaoRemoto = ObterEmailReal(membro.EmailRecuperacao, membro.Email);

        if (!EhEmailReal(usuario.EmailRecuperacao) && emailRecuperacaoRemoto is not null)
        {
            var confirmado = membro.EmailRecuperacaoConfirmado
                && EhEmailReal(membro.EmailRecuperacao)
                && string.Equals(emailRecuperacaoRemoto, membro.EmailRecuperacao, StringComparison.OrdinalIgnoreCase);

            usuario.EmailRecuperacao = emailRecuperacaoRemoto;
            usuario.EmailRecuperacaoConfirmado = confirmado;
            usuario.EmailRecuperacaoConfirmadoEm = confirmado
                ? (membro.AtualizadoEm == default ? DateTime.UtcNow : membro.AtualizadoEm)
                : null;
        }
    }

    private static string? ObterEmailReal(params string?[] candidatos) =>
        candidatos.FirstOrDefault(EhEmailReal)?.Trim();

    private static bool EhEmailReal(string? email)
    {
        return !string.IsNullOrWhiteSpace(email)
            && !email.Trim().EndsWith("@equipe.local", StringComparison.OrdinalIgnoreCase)
            && MailAddress.TryCreate(email.Trim(), out _);
    }

    private static string CriarEmailPlaceholder(string identificador) =>
        $"{identificador.ToLowerInvariant()}@equipe.local";

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

        if (!string.Equals(existente.UserId, userId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"O alias {normalizado} já pertence a outro usuário. Execute a auditoria de segurança antes de sincronizar.");
        }

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
