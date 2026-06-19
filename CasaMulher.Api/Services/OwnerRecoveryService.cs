using CasaMulher.Api.Data;
using CasaMulher.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Services;

public sealed class OwnerRecoveryService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly WebAuthnEnvironmentInfo _webAuthn;
    private readonly ILogger<OwnerRecoveryService> _logger;
    private readonly IAuditoriaService _auditoriaService;

    public OwnerRecoveryService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        WebAuthnEnvironmentInfo webAuthn,
        ILogger<OwnerRecoveryService> logger,
        IAuditoriaService auditoriaService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _webAuthn = webAuthn;
        _logger = logger;
        _auditoriaService = auditoriaService;
    }

    public async Task<OwnerRecoveryResult> ExecuteRecoveryAsync()
    {
        _logger.LogInformation("Iniciando rotina de recuperação de owner.");

        var eqpId = "EQP-000001";
        var admId = "ADM-000003";

        var eqpAlias = await _dbContext.UserLoginIdentifiers
            .FirstOrDefaultAsync(u => u.Identificador == eqpId);

        var admAlias = await _dbContext.UserLoginIdentifiers
            .FirstOrDefaultAsync(u => u.Identificador == admId);

        if (eqpAlias == null || admAlias == null)
        {
            return OwnerRecoveryResult.Failure("Alias EQP-000001 ou ADM-000003 não encontrado no banco de dados.");
        }

        if (eqpAlias.UserId != admAlias.UserId)
        {
            return OwnerRecoveryResult.Failure("Inconsistência grave: EQP-000001 e ADM-000003 não apontam para o mesmo UserId. Abortando.");
        }

        var usuario = await _userManager.FindByIdAsync(eqpAlias.UserId);

        if (usuario == null)
        {
            return OwnerRecoveryResult.Failure("Usuário owner não encontrado pelo ID.");
        }

        var emailCorreto = "odachisamadesu@gmail.com";
        
        usuario.Email = emailCorreto;
        usuario.NormalizedEmail = emailCorreto.ToUpperInvariant();
        usuario.EmailConfirmed = true;
        usuario.EmailRecuperacao = emailCorreto;
        usuario.EmailRecuperacaoConfirmado = true;

        usuario.TwoFactorEnabled = false;

        await _userManager.ResetAuthenticatorKeyAsync(usuario);

        var recoveryCodes = await _dbContext.UserTokens
            .Where(t => t.UserId == usuario.Id && t.LoginProvider == "[AspNetUserStore]" && t.Name == "RecoveryCodes")
            .ToListAsync();

        if (recoveryCodes.Any())
        {
            _dbContext.UserTokens.RemoveRange(recoveryCodes);
        }

        var passkeysInvalidadas = 0;
        var credenciais = await _dbContext.PasskeyCredentials
            .Where(c => c.UserId == usuario.Id && c.RpId == _webAuthn.RpId)
            .ToListAsync();

        if (credenciais.Any())
        {
            _dbContext.PasskeyCredentials.RemoveRange(credenciais);
            passkeysInvalidadas = credenciais.Count;
        }

        var result = await _userManager.UpdateAsync(usuario);
        if (!result.Succeeded)
        {
            return OwnerRecoveryResult.Failure("Falha ao atualizar o usuário owner: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await _dbContext.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "OWNER_RECOVERY_EXECUTADO",
            "ApplicationUser",
            usuario.Id,
            $"Recuperação emergencial executada para owner {eqpId}. 2FA resetado. {passkeysInvalidadas} passkeys locais do RP ID {_webAuthn.RpId} inativadas.");

        _logger.LogInformation("Recuperação do owner concluída com sucesso.");

        return OwnerRecoveryResult.Success(new
        {
            mensagem = "Segurança da conta owner reparada. Entre com ID e senha e configure novamente 2FA/passkey neste ambiente.",
            userId = usuario.Id,
            eqpId,
            admId,
            emailRestaurado = true,
            senhaPreservada = true,
            totpResetado = true,
            passkeysRenderResetadas = true,
            passkeysInvalidadas = passkeysInvalidadas
        });
    }
}

public sealed class OwnerRecoveryResult
{
    public bool IsSuccess { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public object? Payload { get; private set; }

    public static OwnerRecoveryResult Success(object payload) => new() { IsSuccess = true, Payload = payload };
    public static OwnerRecoveryResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}
