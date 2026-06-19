using System.Security.Claims;
using CasaMulher.Api.Security;
using CasaMulher.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    [HttpPost("owner-recovery/reset-security")]
    public async Task<IActionResult> OwnerRecovery(
        [FromServices] OwnerRecoveryService recoveryService,
        [FromHeader(Name = "OWNER_RECOVERY_TOKEN")] string? recoveryToken)
    {
        if (!OwnerAtual())
        {
            return Forbid();
        }

        var configToken = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["OWNER_RECOVERY_TOKEN"];
        if (!string.IsNullOrWhiteSpace(configToken) && recoveryToken != configToken)
        {
            return Unauthorized(new { mensagem = "Token de recuperação inválido ou ausente." });
        }

        var result = await recoveryService.ExecuteRecoveryAsync();

        if (!result.IsSuccess)
        {
            return BadRequest(new { mensagem = result.ErrorMessage });
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
