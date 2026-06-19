using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using CasaMulher.Api.Data;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using CasaMulher.Api.Services;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Controllers;

[ApiController]
[Route("api/passkeys")]
[Authorize]
public class PasskeysController : ControllerBase
{
    private static readonly TimeSpan ChallengeValidade = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFido2 _fido2;
    private readonly IAuditoriaService _auditoriaService;
    private readonly WebAuthnEnvironmentInfo _webAuthn;

    public PasskeysController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IFido2 fido2,
        IAuditoriaService auditoriaService,
        WebAuthnEnvironmentInfo webAuthn)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _fido2 = fido2;
        _auditoriaService = auditoriaService;
        _webAuthn = webAuthn;
    }

    // ── GET /api/passkeys ──────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PasskeyListaItemResponse>>> Listar()
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        var credenciais = await _dbContext.PasskeyCredentials
            .Where(c => c.UserId == usuario.Id && c.RpId == _webAuthn.RpId)
            .OrderByDescending(c => c.CriadoEm)
            .Select(c => new PasskeyListaItemResponse
            {
                Id = c.Id,
                NomeDispositivo = c.NomeDispositivo,
                CriadoEm = c.CriadoEm,
                UltimoUsoEm = c.UltimoUsoEm
            })
            .ToListAsync();

        return Ok(credenciais);
    }

    // ── POST /api/passkeys/registrar/iniciar ───────────────────────────────

    [HttpPost("registrar/iniciar")]
    public async Task<ActionResult<PasskeyRegistrarIniciarResponse>> RegistrarIniciar()
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        // Credenciais já cadastradas — excluir para evitar duplicatas durante attestation
        var credenciaisExistentes = await _dbContext.PasskeyCredentials
            .Where(c => c.UserId == usuario.Id && c.RpId == _webAuthn.RpId)
            .Select(c => c.CredentialId)
            .ToListAsync();

        var excludeCredentials = credenciaisExistentes
            .Select(id => new PublicKeyCredentialDescriptor(id))
            .ToList();

        var fidoUser = new Fido2User
        {
            Id = System.Text.Encoding.UTF8.GetBytes(usuario.Id),
            Name = usuario.IdentificadorFuncionario,
            DisplayName = usuario.NomeCompleto
        };

        // Passkeys precisam ser discoverable para login sem digitar ID
        // RequireResidentKey = true força credencial resident/discoverable
        var authenticatorSelection = new AuthenticatorSelection
        {
            RequireResidentKey = true,
            UserVerification = UserVerificationRequirement.Required
        };

        var options = _fido2.RequestNewCredential(
            fidoUser,
            excludeCredentials,
            authenticatorSelection,
            AttestationConveyancePreference.None);

        var challengeId = Guid.NewGuid().ToString("N");
        var optionsJson = options.ToJson();

        _dbContext.PasskeyChallenges.Add(new PasskeyChallenge
        {
            ChallengeId = challengeId,
            ChallengeBytes = options.Challenge,
            Tipo = "Registro",
            OptionsJson = optionsJson,
            UserId = usuario.Id,
            CriadoEm = DateTime.UtcNow,
            ExpiracaoEm = DateTime.UtcNow.Add(ChallengeValidade)
        });

        await _dbContext.SaveChangesAsync();

        return Ok(new PasskeyRegistrarIniciarResponse
        {
            ChallengeId = challengeId,
            PublicKeyOptions = JsonNode.Parse(optionsJson)
        });
    }

    // ── POST /api/passkeys/registrar/concluir ──────────────────────────────

    [HttpPost("registrar/concluir")]
    public async Task<IActionResult> RegistrarConcluir(PasskeyRegistrarConcluirRequest request)
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.ChallengeId))
        {
            return BadRequest(new { mensagem = "ChallengeId inválido." });
        }

        var challenge = await _dbContext.PasskeyChallenges
            .SingleOrDefaultAsync(c => c.ChallengeId == request.ChallengeId && c.Tipo == "Registro");

        if (challenge is null || challenge.UserId != usuario.Id || challenge.ExpiracaoEm < DateTime.UtcNow)
        {
            return BadRequest(new { mensagem = "Sessão de cadastro expirada ou inválida. Tente novamente." });
        }

        CredentialCreateOptions options;

        try
        {
            options = CredentialCreateOptions.FromJson(challenge.OptionsJson);
        }
        catch
        {
            return BadRequest(new { mensagem = "Não foi possível recuperar o contexto de cadastro." });
        }

        if (request.Credential is null)
        {
            return BadRequest(new { mensagem = "Credencial não informada." });
        }

        AuthenticatorAttestationRawResponse attestationResponse;

        try
        {
            var credJson = request.Credential.ToJsonString();
            attestationResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(credJson)
                ?? throw new InvalidOperationException("Deserialização retornou null.");
        }
        catch
        {
            return BadRequest(new { mensagem = "Formato da credencial inválido." });
        }

        IsCredentialIdUniqueToUserAsyncDelegate isCredentialIdUniqueToUser = async (args, _) =>
        {
            return !await _dbContext.PasskeyCredentials
                .AnyAsync(c => c.CredentialId == args.CredentialId);
        };

        Fido2.CredentialMakeResult result;

        try
        {
            result = await _fido2.MakeNewCredentialAsync(
                attestationResponse,
                options,
                isCredentialIdUniqueToUser);
        }
        catch (Fido2VerificationException ex)
        {
            await _auditoriaService.RegistrarAsync(
                "PASSKEY_CRIADA_FALHA",
                "PasskeyCredential",
                usuario.Id,
                $"Falha ao cadastrar passkey para {usuario.IdentificadorFuncionario}: {ex.Message}");

            return BadRequest(new { mensagem = "Não foi possível verificar a chave de acesso." });
        }

        var nomePadrao = string.IsNullOrWhiteSpace(request.NomeDispositivo)
            ? "Dispositivo"
            : request.NomeDispositivo.Trim();

        _dbContext.PasskeyCredentials.Add(new PasskeyCredential
        {
            UserId = usuario.Id,
            CredentialId = result.Result!.CredentialId,
            PublicKey = result.Result.PublicKey,
            SignatureCounter = result.Result.Counter,
            NomeDispositivo = nomePadrao,
            RpId = _webAuthn.RpId,
            CriadoEm = DateTime.UtcNow
        });

        _dbContext.PasskeyChallenges.Remove(challenge);
        await _dbContext.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "PASSKEY_CRIADA",
            "PasskeyCredential",
            usuario.Id,
            $"Chave de acesso '{nomePadrao}' cadastrada para {usuario.IdentificadorFuncionario}.");

        return Ok(new { mensagem = "Chave de acesso cadastrada com sucesso." });
    }

    // ── DELETE /api/passkeys/{id} ──────────────────────────────────────────

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id)
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        var credencial = await _dbContext.PasskeyCredentials
            .SingleOrDefaultAsync(c => c.Id == id && c.UserId == usuario.Id);

        if (credencial is null)
        {
            return NotFound(new { mensagem = "Chave de acesso não encontrada." });
        }

        var nomeDispositivo = credencial.NomeDispositivo ?? "Dispositivo";
        _dbContext.PasskeyCredentials.Remove(credencial);
        await _dbContext.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "PASSKEY_REMOVIDA",
            "PasskeyCredential",
            usuario.Id,
            $"Chave de acesso '{nomeDispositivo}' removida por {usuario.IdentificadorFuncionario}.");

        return Ok(new { mensagem = "Chave de acesso removida." });
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<ApplicationUser?> ObterUsuarioAtual()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return null;
        }

        return await _userManager.FindByIdAsync(usuarioId);
    }
}
