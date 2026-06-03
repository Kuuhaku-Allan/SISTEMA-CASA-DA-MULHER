using CasaMulher.Api.Data;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using CasaMulher.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAcesso.SomenteAdm)]
[Route("api/funcionarios")]
public class FuncionariosController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ISenhaTemporariaService _senhaTemporariaService;
    private readonly IAuditoriaService _auditoriaService;

    public FuncionariosController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ISenhaTemporariaService senhaTemporariaService,
        IAuditoriaService auditoriaService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _senhaTemporariaService = senhaTemporariaService;
        _auditoriaService = auditoriaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FuncionarioAdminResponse>>> Listar()
    {
        var funcionarios = await _dbContext.Users
            .OrderBy(usuario => usuario.IdentificadorFuncionario)
            .ToListAsync();

        return Ok(funcionarios.Select(MapearFuncionario));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FuncionarioAdminResponse>> ObterPorId(string id)
    {
        var funcionario = await _userManager.FindByIdAsync(id);

        if (funcionario is null)
        {
            return NotFound(new { mensagem = "Funcionário não encontrado." });
        }

        return Ok(MapearFuncionario(funcionario));
    }

    [HttpPatch("{id}/desativar")]
    public async Task<ActionResult<FuncionarioAdminResponse>> Desativar(string id)
    {
        var funcionario = await _userManager.FindByIdAsync(id);

        if (funcionario is null)
        {
            return NotFound(new { mensagem = "Funcionário não encontrado." });
        }

        funcionario.Ativo = false;
        await _userManager.UpdateAsync(funcionario);
        await _auditoriaService.RegistrarAsync(
            "FUNCIONARIO_DESATIVADO",
            "ApplicationUser",
            funcionario.Id,
            $"Desativou o funcionário {funcionario.IdentificadorFuncionario} ({funcionario.Email}).");

        return Ok(MapearFuncionario(funcionario));
    }

    [HttpPatch("{id}/reativar")]
    public async Task<ActionResult<FuncionarioAdminResponse>> Reativar(string id)
    {
        var funcionario = await _userManager.FindByIdAsync(id);

        if (funcionario is null)
        {
            return NotFound(new { mensagem = "Funcionário não encontrado." });
        }

        funcionario.Ativo = true;
        await _userManager.UpdateAsync(funcionario);
        await _auditoriaService.RegistrarAsync(
            "FUNCIONARIO_REATIVADO",
            "ApplicationUser",
            funcionario.Id,
            $"Reativou o funcionário {funcionario.IdentificadorFuncionario} ({funcionario.Email}).");

        return Ok(MapearFuncionario(funcionario));
    }

    [HttpPatch("{id}/alterar-perfil")]
    public async Task<ActionResult<FuncionarioAdminResponse>> AlterarPerfil(string id, AlterarPerfilFuncionarioRequest request)
    {
        var novoPerfil = request.Perfil.Trim().ToLowerInvariant();

        if (!PerfisAcesso.EhValido(novoPerfil))
        {
            return BadRequest(new { mensagem = "Perfil inválido." });
        }

        var funcionario = await _userManager.FindByIdAsync(id);

        if (funcionario is null)
        {
            return NotFound(new { mensagem = "Funcionário não encontrado." });
        }

        if (!await _roleManager.RoleExistsAsync(novoPerfil))
        {
            await _roleManager.CreateAsync(new IdentityRole(novoPerfil));
        }

        var perfilAnterior = funcionario.Perfil;
        var rolesAtuais = await _userManager.GetRolesAsync(funcionario);

        if (rolesAtuais.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(funcionario, rolesAtuais);
        }

        await _userManager.AddToRoleAsync(funcionario, novoPerfil);

        funcionario.Perfil = novoPerfil;
        funcionario.DoisFatoresObrigatorio = PerfilExigeDoisFatores(novoPerfil);
        await _userManager.UpdateAsync(funcionario);
        await _auditoriaService.RegistrarAsync(
            "PERFIL_ALTERADO",
            "ApplicationUser",
            funcionario.Id,
            $"Alterou perfil de {funcionario.IdentificadorFuncionario} de {perfilAnterior} para {novoPerfil}.");

        return Ok(MapearFuncionario(funcionario));
    }

    [HttpPost("{id}/resetar-senha")]
    public async Task<ActionResult<ResetarSenhaFuncionarioResponse>> ResetarSenha(string id)
    {
        var funcionario = await _userManager.FindByIdAsync(id);

        if (funcionario is null)
        {
            return NotFound(new { mensagem = "Funcionário não encontrado." });
        }

        var senhaTemporaria = _senhaTemporariaService.Gerar();
        var token = await _userManager.GeneratePasswordResetTokenAsync(funcionario);
        var result = await _userManager.ResetPasswordAsync(funcionario, token, senhaTemporaria);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Não foi possível redefinir a senha.",
                erros = result.Errors.Select(error => error.Description)
            });
        }

        funcionario.DeveTrocarSenha = true;
        await _userManager.UpdateAsync(funcionario);
        await _auditoriaService.RegistrarAsync(
            "SENHA_RESETADA",
            "ApplicationUser",
            funcionario.Id,
            $"Redefiniu a senha do funcionário {funcionario.IdentificadorFuncionario} ({funcionario.Email}).");

        return Ok(new ResetarSenhaFuncionarioResponse
        {
            Mensagem = "Senha temporária gerada com sucesso.",
            SenhaTemporaria = senhaTemporaria,
            DeveTrocarSenha = true
        });
    }

    [HttpPost("{id}/resetar-2fa")]
    public async Task<IActionResult> ResetarDoisFatores(string id)
    {
        var funcionario = await _userManager.FindByIdAsync(id);

        if (funcionario is null)
        {
            return NotFound(new { mensagem = "Funcionário não encontrado." });
        }

        await _userManager.SetTwoFactorEnabledAsync(funcionario, false);
        await _userManager.ResetAuthenticatorKeyAsync(funcionario);
        await _auditoriaService.RegistrarAsync(
            "DOIS_FATORES_RESETADO",
            "ApplicationUser",
            funcionario.Id,
            $"Redefiniu o autenticador 2FA do funcionário {funcionario.IdentificadorFuncionario} ({funcionario.Email}).");

        return Ok(new { mensagem = "Autenticador redefinido com sucesso." });
    }

    private static FuncionarioAdminResponse MapearFuncionario(ApplicationUser funcionario)
    {
        return new FuncionarioAdminResponse
        {
            Id = funcionario.Id,
            IdentificadorFuncionario = funcionario.IdentificadorFuncionario,
            NomeCompleto = funcionario.NomeCompleto,
            Email = funcionario.Email ?? string.Empty,
            Perfil = funcionario.Perfil,
            Ativo = funcionario.Ativo,
            DoisFatoresAtivo = funcionario.TwoFactorEnabled,
            DoisFatoresObrigatorio = funcionario.DoisFatoresObrigatorio,
            DeveTrocarSenha = funcionario.DeveTrocarSenha,
            CriadoEm = funcionario.CriadoEm
        };
    }

    private static bool PerfilExigeDoisFatores(string perfil)
    {
        return string.Equals(perfil, PerfisAcesso.Adm, StringComparison.OrdinalIgnoreCase)
            || string.Equals(perfil, PerfisAcesso.Juridico, StringComparison.OrdinalIgnoreCase)
            || string.Equals(perfil, PerfisAcesso.AssistenteSocial, StringComparison.OrdinalIgnoreCase);
    }
}
