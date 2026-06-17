using System.Security.Claims;
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
    private readonly IAuditoriaService _auditoriaService;
    private readonly IRedefinicaoSenhaEmailService _redefinicaoSenhaEmailService;
    private readonly IMasterUserService _masterUserService;

    public FuncionariosController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditoriaService auditoriaService,
        IRedefinicaoSenhaEmailService redefinicaoSenhaEmailService,
        IMasterUserService masterUserService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _auditoriaService = auditoriaService;
        _redefinicaoSenhaEmailService = redefinicaoSenhaEmailService;
        _masterUserService = masterUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FuncionarioAdminResponse>>> Listar()
    {
        if (!await PodeGerenciarFuncionariosInstitucionaisAsync())
        {
            return Forbid();
        }

        var funcionarios = await _dbContext.Users
            .Where(usuario => usuario.Perfil != PerfisAcesso.Equipe)
            .OrderBy(usuario => usuario.IdentificadorFuncionario)
            .ToListAsync();

        return Ok(funcionarios.Select(MapearFuncionario));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FuncionarioAdminResponse>> ObterPorId(string id)
    {
        if (!await PodeGerenciarFuncionariosInstitucionaisAsync())
        {
            return Forbid();
        }

        var funcionario = await _userManager.FindByIdAsync(id);

        if (funcionario is null)
        {
            return NotFound(new { mensagem = "Funcionário não encontrado." });
        }

        if (EhContaEquipe(funcionario))
        {
            return NotFound(new { mensagem = "Funcionario nao encontrado." });
        }

        return Ok(MapearFuncionario(funcionario));
    }

    [HttpPatch("{id}/desativar")]
    public async Task<ActionResult<FuncionarioAdminResponse>> Desativar(string id)
    {
        if (!await PodeGerenciarFuncionariosInstitucionaisAsync())
        {
            return Forbid();
        }

        var funcionario = await _userManager.FindByIdAsync(id);

        if (funcionario is null)
        {
            return NotFound(new { mensagem = "Funcionário não encontrado." });
        }

        if (EhContaEquipe(funcionario))
        {
            return NotFound(new { mensagem = "Funcionario nao encontrado." });
        }

        if (!await PodeExecutarAcaoFuncionarioAsync(funcionario))
        {
            return Forbid();
        }

        var bloqueioDesativacao = await ValidarDesativacaoSeguraAsync(funcionario);

        if (bloqueioDesativacao is not null)
        {
            return bloqueioDesativacao;
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
        if (!await PodeGerenciarFuncionariosInstitucionaisAsync())
        {
            return Forbid();
        }

        var funcionario = await _userManager.FindByIdAsync(id);

        if (funcionario is null)
        {
            return NotFound(new { mensagem = "Funcionário não encontrado." });
        }

        if (EhContaEquipe(funcionario))
        {
            return NotFound(new { mensagem = "Funcionario nao encontrado." });
        }

        if (!await PodeExecutarAcaoFuncionarioAsync(funcionario))
        {
            return Forbid();
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
        if (!await PodeGerenciarFuncionariosInstitucionaisAsync())
        {
            return Forbid();
        }

        var novoPerfil = request.Perfil.Trim().ToLowerInvariant();

        if (!PerfisAcesso.EhFuncionarioInstitucionalValido(novoPerfil))
        {
            return BadRequest(new { mensagem = "Perfil inválido." });
        }

        var funcionario = await _userManager.FindByIdAsync(id);

        if (funcionario is null)
        {
            return NotFound(new { mensagem = "Funcionário não encontrado." });
        }

        if (EhContaEquipe(funcionario))
        {
            return NotFound(new { mensagem = "Funcionario nao encontrado." });
        }

        if (!await PodeExecutarAcaoFuncionarioAsync(funcionario, novoPerfil))
        {
            return Forbid();
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
        return await EnviarRedefinicaoSenhaPorEmail(id);
    }

    [HttpPost("{id}/enviar-redefinicao-senha")]
    public async Task<ActionResult<ResetarSenhaFuncionarioResponse>> EnviarRedefinicaoSenha(string id)
    {
        return await EnviarRedefinicaoSenhaPorEmail(id);
    }

    private async Task<ActionResult<ResetarSenhaFuncionarioResponse>> EnviarRedefinicaoSenhaPorEmail(string id)
    {
        if (!await PodeGerenciarFuncionariosInstitucionaisAsync())
        {
            return Forbid();
        }

        var funcionario = await _userManager.FindByIdAsync(id);

        if (funcionario is null)
        {
            return NotFound(new { mensagem = "Funcionário não encontrado." });
        }

        if (EhContaEquipe(funcionario))
        {
            return NotFound(new { mensagem = "Funcionario nao encontrado." });
        }

        if (!await PodeExecutarAcaoFuncionarioAsync(funcionario))
        {
            return Forbid();
        }

        var resultadoEmail = await _redefinicaoSenhaEmailService.EnviarAsync(funcionario);
        await _auditoriaService.RegistrarAsync(
            "REDEFINICAO_SENHA_SOLICITADA",
            "ApplicationUser",
            funcionario.Id,
            $"Solicitou redefinição de senha para {funcionario.IdentificadorFuncionario} ({funcionario.Email}). Status do e-mail: {resultadoEmail.StatusEmail ?? "Não informado"}.");

        return Ok(new ResetarSenhaFuncionarioResponse
        {
            Mensagem = resultadoEmail.EmailEnviado
                ? "Link de redefinição enviado para o e-mail do funcionário."
                : resultadoEmail.AvisoEmail ?? "Não foi possível enviar o link de redefinição de senha.",
            EmailEnviado = resultadoEmail.EmailEnviado,
            StatusEmail = resultadoEmail.StatusEmail,
            AvisoEmail = resultadoEmail.AvisoEmail
        });
    }

    [HttpPost("{id}/resetar-2fa")]
    public async Task<IActionResult> ResetarDoisFatores(string id)
    {
        if (!await PodeGerenciarFuncionariosInstitucionaisAsync())
        {
            return Forbid();
        }

        var funcionario = await _userManager.FindByIdAsync(id);

        if (funcionario is null)
        {
            return NotFound(new { mensagem = "Funcionário não encontrado." });
        }

        if (EhContaEquipe(funcionario))
        {
            return NotFound(new { mensagem = "Funcionario nao encontrado." });
        }

        if (!await PodeExecutarAcaoFuncionarioAsync(funcionario))
        {
            return Forbid();
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

    private async Task<bool> PodeGerenciarFuncionariosInstitucionaisAsync()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return false;
        }

        var usuario = await _userManager.FindByIdAsync(usuarioId);

        if (usuario is null || !usuario.Ativo)
        {
            return false;
        }

        if (string.Equals(usuario.Perfil, PerfisAcesso.Adm, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.Equals(usuario.Perfil, PerfisAcesso.Equipe, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return await _dbContext.EquipeMembros.AnyAsync(membro =>
            membro.UserId == usuario.Id
            && membro.Ativo
            && membro.PapelEquipe == EquipePapeis.Owner
            && membro.CodigoEquipe == _masterUserService.EquipeOwnerCodigo);
    }

    private async Task<bool> PodeExecutarAcaoFuncionarioAsync(ApplicationUser funcionario, string? novoPerfil = null)
    {
        if (EhContaEquipe(funcionario))
        {
            return false;
        }

        if (await UsuarioAtualEhMasterAsync())
        {
            return true;
        }

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return false;
        }

        var usuarioAtual = await _userManager.FindByIdAsync(usuarioId);

        if (usuarioAtual is null
            || !usuarioAtual.Ativo
            || !string.Equals(usuarioAtual.Perfil, PerfisAcesso.Adm, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(funcionario.Perfil, PerfisAcesso.Adm, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(novoPerfil, PerfisAcesso.Adm, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private async Task<bool> UsuarioAtualEhMasterAsync()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return false;
        }

        var usuario = await _userManager.FindByIdAsync(usuarioId);

        if (_masterUserService.EhSuperAdminInstitucional(usuario))
        {
            return true;
        }

        if (usuario is null
            || !usuario.Ativo
            || !string.Equals(usuario.Perfil, PerfisAcesso.Equipe, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return await _dbContext.EquipeMembros.AnyAsync(membro =>
            membro.UserId == usuario.Id
            && membro.Ativo
            && membro.PapelEquipe == EquipePapeis.Owner
            && membro.CodigoEquipe == _masterUserService.EquipeOwnerCodigo);
    }

    private async Task<ActionResult?> ValidarDesativacaoSeguraAsync(ApplicationUser funcionario)
    {
        if (_masterUserService.EhSuperAdminInstitucional(funcionario))
        {
            return BadRequest(new { mensagem = "Nao e possivel desativar o super admin master configurado." });
        }

        if (!string.Equals(funcionario.Perfil, PerfisAcesso.Adm, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var existeOutroAdmAtivo = await _dbContext.Users.AnyAsync(usuario =>
            usuario.Id != funcionario.Id
            && usuario.Ativo
            && usuario.Perfil == PerfisAcesso.Adm);

        if (existeOutroAdmAtivo)
        {
            return null;
        }

        return BadRequest(new { mensagem = "Nao e possivel desativar o ultimo super admin ativo." });
    }

    private static bool EhContaEquipe(ApplicationUser funcionario)
    {
        return string.Equals(funcionario.Perfil, PerfisAcesso.Equipe, StringComparison.OrdinalIgnoreCase)
            || funcionario.IdentificadorFuncionario.StartsWith("EQP-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool PerfilExigeDoisFatores(string perfil)
    {
        return string.Equals(perfil, PerfisAcesso.Adm, StringComparison.OrdinalIgnoreCase)
            || string.Equals(perfil, PerfisAcesso.Juridico, StringComparison.OrdinalIgnoreCase)
            || string.Equals(perfil, PerfisAcesso.AssistenteSocial, StringComparison.OrdinalIgnoreCase);
    }
}
