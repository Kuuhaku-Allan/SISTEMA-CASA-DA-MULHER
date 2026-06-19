using System.Security.Claims;
using CasaMulher.Api.Security;
using CasaMulher.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Identity;
using CasaMulher.Api.Data;
using CasaMulher.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/homologacao")]
public sealed class HomologacaoController : ControllerBase
{
    private readonly HmlDbSnapshotService _snapshot;
    private readonly IMasterUserService _masterUser;
    private readonly HomologacaoSeedService _seed;

    public HomologacaoController(
        HmlDbSnapshotService snapshot,
        IMasterUserService masterUser,
        HomologacaoSeedService seed)
    {
        _snapshot = snapshot;
        _masterUser = masterUser;
        _seed = seed;
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        var status = _snapshot.GetStatus();
        return Ok(new
        {
            staging = status.Staging,
            snapshotHabilitado = status.EnabledRequested,
            snapshotConfigurado = status.Configured,
            status.Repository,
            status.SnapshotPath,
            status.Message,
            podeGerenciar = OwnerAtual()
        });
    }

    [HttpPost("snapshot")]
    public async Task<IActionResult> Snapshot(CancellationToken cancellationToken)
    {
        if (!OwnerAtual()) return Forbid();
        try
        {
            await _snapshot.CreateAndUploadAsync(cancellationToken);
            return Ok(new { mensagem = "Snapshot criptografado de homologação atualizado." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensagem = $"Erro ao gerar snapshot: {ex.Message}" });
        }
    }

    [HttpGet("recepcao-seed")]
    public async Task<IActionResult> RecepcaoSeed(CancellationToken cancellationToken)
    {
        var document = await _seed.LoadAsync(cancellationToken);
        return Ok(document?.Recepcao ?? []);
    }

    [AllowAnonymous]
    [HttpGet("owner-recovery/security-diagnostics")]
    public async Task<IActionResult> SecurityDiagnostics(
        [FromServices] Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtectionProvider,
        [FromServices] GitHubPortalSessionStore sessionStore,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] AppDbContext dbContext,
        [FromServices] HmlDbSnapshotService snapshotService,
        [FromServices] WebAuthnEnvironmentInfo webAuthnInfo)
    {
        var cookie = Request.Cookies[CasaMulher.Api.Middleware.RenderAccessGateMiddleware.AuthCookieName];
        if (string.IsNullOrWhiteSpace(cookie)) return Unauthorized(new { mensagem = "Sessão do GitHub não encontrada." });

        GitHubPortalSession? session = null;
        try
        {
            var protector = dataProtectionProvider.CreateProtector(CasaMulher.Api.Middleware.RenderAccessGateMiddleware.ProtectorPurpose);
            var sessionId = protector.Unprotect(cookie);
            if (!sessionStore.TryGet(sessionId, out session) || session is null)
                return Unauthorized(new { mensagem = "Sessão do GitHub inválida ou expirada." });
        }
        catch
        {
            return Unauthorized(new { mensagem = "Sessão do GitHub corrompida." });
        }

        var expectedOwner = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("GitHub:OwnerLogin", HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("GITHUB_OWNER_LOGIN", "Kuuhaku-Allan"));
        if (!string.Equals(session.GitHubUsername, expectedOwner, StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = "Apenas o Owner do GitHub configurado pode executar esta ação." });

        var eqpAlias = await dbContext.UserLoginIdentifiers.FirstOrDefaultAsync(u => u.Identificador == "EQP-000001");
        var admAlias = await dbContext.UserLoginIdentifiers.FirstOrDefaultAsync(u => u.Identificador == "ADM-000003");

        var mesmoUserId = eqpAlias?.UserId == admAlias?.UserId;
        var usuario = eqpAlias != null ? await userManager.FindByIdAsync(eqpAlias.UserId) : null;

        var snapshotStatus = snapshotService.GetStatus();

        if (usuario == null)
        {
            return NotFound(new { mensagem = "Usuário Owner não encontrado no banco." });
        }

        var authenticatorKey = await userManager.GetAuthenticatorKeyAsync(usuario);
        var recoveryCodes = await dbContext.UserTokens
            .Where(t => t.UserId == usuario.Id && t.LoginProvider == "[AspNetUserStore]" && t.Name == "RecoveryCodes")
            .CountAsync();

        var passkeys = await dbContext.PasskeyCredentials
            .Where(c => c.UserId == usuario.Id)
            .GroupBy(c => c.RpId)
            .Select(g => new { RpId = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            userId = usuario.Id,
            eqpId = "EQP-000001",
            admId = "ADM-000003",
            mesmoUserId,
            twoFactorEnabled = usuario.TwoFactorEnabled,
            authenticatorKeyExiste = !string.IsNullOrWhiteSpace(authenticatorKey),
            recoveryCodesCount = recoveryCodes,
            passkeysCount = passkeys.Sum(p => p.Count),
            passkeysPorRpId = passkeys,
            rpIdAtual = webAuthnInfo.RpId,
            email = usuario.Email,
            emailRecuperacao = usuario.EmailRecuperacao,
            snapshotAtivo = snapshotStatus.EnabledRequested && snapshotStatus.Configured
        });
    }

    [AllowAnonymous]
    [HttpGet("owner-recovery/status")]
    public IActionResult OwnerRecoveryStatus([FromServices] Microsoft.Extensions.Caching.Memory.IMemoryCache cache, [FromServices] Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtectionProvider, [FromServices] GitHubPortalSessionStore sessionStore)
    {
        var cookie = Request.Cookies[CasaMulher.Api.Middleware.RenderAccessGateMiddleware.AuthCookieName];
        if (string.IsNullOrWhiteSpace(cookie)) return Unauthorized(new { mensagem = "Sessão do GitHub não encontrada." });

        GitHubPortalSession? session = null;
        try
        {
            var protector = dataProtectionProvider.CreateProtector(CasaMulher.Api.Middleware.RenderAccessGateMiddleware.ProtectorPurpose);
            var sessionId = protector.Unprotect(cookie);
            if (!sessionStore.TryGet(sessionId, out session) || session is null)
                return Unauthorized(new { mensagem = "Sessão do GitHub inválida ou expirada." });
        }
        catch
        {
            return Unauthorized(new { mensagem = "Sessão do GitHub corrompida." });
        }

        var expectedOwner = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("GitHub:OwnerLogin", HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("GITHUB_OWNER_LOGIN", "Kuuhaku-Allan"));
        
        if (!string.Equals(session.GitHubUsername, expectedOwner, StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = "Apenas o Owner do GitHub configurado pode executar esta ação." });

        var configToken = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["OWNER_RECOVERY_TOKEN"];
        
        var nonce = Guid.NewGuid().ToString("N");
        var cacheKey = $"OwnerRecoveryNonce_{session.GitHubId}";
        cache.Set(cacheKey, nonce, TimeSpan.FromMinutes(10));

        return Ok(new
        {
            disponivel = true,
            ambiente = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().EnvironmentName,
            ownerGitHub = expectedOwner,
            usuarioGitHubAtual = session.GitHubUsername,
            autorizado = true,
            tokenObrigatorio = !string.IsNullOrWhiteSpace(configToken),
            eqpId = "EQP-000001",
            admId = "ADM-000003",
            nonce = nonce
        });
    }

    [AllowAnonymous]
    [HttpPost("owner-recovery/reset-security")]
    public async Task<IActionResult> OwnerRecovery(
        [FromServices] OwnerRecoveryService recoveryService,
        [FromServices] Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtectionProvider,
        [FromServices] GitHubPortalSessionStore sessionStore,
        [FromServices] Microsoft.Extensions.Caching.Memory.IMemoryCache cache,
        [FromBody] OwnerRecoveryRequest request)
    {
        if (request is null || request.Confirmacao != "RESETAR_SEGURANCA_OWNER")
            return BadRequest(new { mensagem = "Confirmação textual obrigatória inválida." });

        if (string.IsNullOrWhiteSpace(request.Nonce))
            return BadRequest(new { mensagem = "Nonce de segurança ausente." });

        var cookie = Request.Cookies[CasaMulher.Api.Middleware.RenderAccessGateMiddleware.AuthCookieName];
        if (string.IsNullOrWhiteSpace(cookie)) return Unauthorized(new { mensagem = "Sessão do GitHub não encontrada." });

        GitHubPortalSession? session = null;
        try
        {
            var protector = dataProtectionProvider.CreateProtector(CasaMulher.Api.Middleware.RenderAccessGateMiddleware.ProtectorPurpose);
            var sessionId = protector.Unprotect(cookie);
            if (!sessionStore.TryGet(sessionId, out session) || session is null)
                return Unauthorized(new { mensagem = "Sessão do GitHub inválida ou expirada." });

            var expectedOwner = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("GitHub:OwnerLogin", HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("GITHUB_OWNER_LOGIN", "Kuuhaku-Allan"));
            
            if (!string.Equals(session.GitHubUsername, expectedOwner, StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = "Apenas o Owner do GitHub configurado pode executar esta ação." });
        }
        catch
        {
            return Unauthorized(new { mensagem = "Sessão do GitHub corrompida." });
        }

        var cacheKey = $"OwnerRecoveryNonce_{session.GitHubId}";
        if (!cache.TryGetValue(cacheKey, out string? cachedNonce) || cachedNonce != request.Nonce)
        {
            return BadRequest(new { mensagem = "Nonce inválido ou expirado." });
        }
        cache.Remove(cacheKey);

        var configToken = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["OWNER_RECOVERY_TOKEN"];
        if (!string.IsNullOrWhiteSpace(configToken) && request.OwnerRecoveryToken != configToken)
        {
            return Unauthorized(new { mensagem = "Token de recuperação inválido ou ausente." });
        }

        var result = await recoveryService.ExecuteRecoveryAsync(session.GitHubUsername);

        if (!result.IsSuccess)
        {
            return BadRequest(new { mensagem = result.ErrorMessage });
        }

        try
        {
            var snapshotService = HttpContext.RequestServices.GetRequiredService<HmlDbSnapshotService>();
            var snapshotStatus = snapshotService.GetStatus();
            
            if (snapshotStatus.EnabledRequested && snapshotStatus.Configured)
            {
                await snapshotService.CreateAndUploadAsync(CancellationToken.None);
                return Ok(new { mensagem = result.Payload?.ToString() + " Snapshot manual gerado com sucesso." });
            }
        }
        catch (Exception)
        {
            return Ok(new { mensagem = result.Payload?.ToString() + " IMPORTANTE: Recuperação aplicada, mas o snapshot automático falhou. Gere o snapshot manualmente pelo painel." });
        }

        return Ok(result.Payload);
    }
    private bool OwnerAtual()
    {
        var identificador = User.FindFirstValue("identificadorFuncionario");
        return _masterUser.EhEquipeOwnerPrincipal(identificador)
            || string.Equals(identificador, _masterUser.SuperAdminIdentificador, StringComparison.OrdinalIgnoreCase);
    }
}

public class OwnerRecoveryRequest
{
    public string Confirmacao { get; set; } = string.Empty;
    public string? OwnerRecoveryToken { get; set; }
    public string Nonce { get; set; } = string.Empty;
}
