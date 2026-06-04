using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CasaMulher.Api.Data;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using CasaMulher.Api.Services;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CasaMulher.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private sealed record JwtEmitido(string Token, DateTime ExpiraEm);

    private const string AuthenticatorIssuer = "Casa da Mulher";
    private static readonly TimeSpan LoginTemporarioValidade = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PasskeyChallengeValidade = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PasskeyReconfirmacaoValidade = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PasskeyReconfirmacaoPrazo = TimeSpan.FromDays(7);

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConviteCodigoService _codigoService;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IRedefinicaoSenhaEmailService _redefinicaoSenhaEmailService;
    private readonly IEmailRecuperacaoEmailService _emailRecuperacaoEmailService;
    private readonly IRedefinicaoSenhaThrottleService _redefinicaoSenhaThrottleService;
    private readonly IConfiguration _configuration;
    private readonly IDataProtector _loginDoisFatoresProtector;
    private readonly IFido2 _fido2;

    public AuthController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConviteCodigoService codigoService,
        IAuditoriaService auditoriaService,
        IRedefinicaoSenhaEmailService redefinicaoSenhaEmailService,
        IEmailRecuperacaoEmailService emailRecuperacaoEmailService,
        IRedefinicaoSenhaThrottleService redefinicaoSenhaThrottleService,
        IConfiguration configuration,
        IDataProtectionProvider dataProtectionProvider,
        IFido2 fido2)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _codigoService = codigoService;
        _auditoriaService = auditoriaService;
        _redefinicaoSenhaEmailService = redefinicaoSenhaEmailService;
        _emailRecuperacaoEmailService = emailRecuperacaoEmailService;
        _redefinicaoSenhaThrottleService = redefinicaoSenhaThrottleService;
        _configuration = configuration;
        _loginDoisFatoresProtector = dataProtectionProvider.CreateProtector("CasaMulher.LoginDoisFatores");
        _fido2 = fido2;
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.ConvitePublico)]
    [HttpGet("convite-publico")]
    public async Task<ActionResult<ConvitePublicoResponse>> ObterConvitePublico([FromQuery] string email, [FromQuery] string codigo)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(codigo))
        {
            await RegistrarConvitePublicoInvalidoAsync("Consulta pública de convite sem dados obrigatórios.");
            return BadRequest(new { mensagem = "Informe o e-mail e o código do convite." });
        }

        var convite = await ObterConvitePorCodigoAsync(codigo);
        var erroConvite = ValidarConviteParaFinalizacao(convite, email.Trim());

        if (erroConvite is not null)
        {
            await RegistrarConvitePublicoInvalidoAsync("Consulta pública de convite inválida. Nenhum código de convite foi registrado.");
            return erroConvite;
        }

        return Ok(new ConvitePublicoResponse
        {
            NomeCompleto = convite!.NomeCompleto,
            Email = convite.Email,
            IdentificadorFuncionario = convite.IdentificadorFuncionario,
            ExpiraEm = convite.ExpiraEm
        });
    }

    [AllowAnonymous]
    [HttpPost("register-funcionario")]
    public async Task<IActionResult> RegisterFuncionario(RegisterFuncionarioRequest request)
    {
        if (request.Senha != request.ConfirmarSenha)
        {
            return BadRequest(new { mensagem = "Senha e confirmação de senha não conferem." });
        }

        var email = request.Email.Trim();
        var convite = await ObterConvitePorCodigoAsync(request.CodigoCadastro);
        var erroConvite = ValidarConviteParaFinalizacao(convite, email);

        if (erroConvite is not null)
        {
            return erroConvite;
        }

        var usuarioExistente = await _userManager.FindByEmailAsync(email);

        if (usuarioExistente is not null)
        {
            return BadRequest(new { mensagem = "Já existe usuário cadastrado com este e-mail." });
        }

        var identificadorFuncionario = convite!.IdentificadorFuncionario.Trim();
        var identificadorNormalizado = identificadorFuncionario.ToUpperInvariant();
        var identificadorEmUso = await _dbContext.Users.AnyAsync(usuario =>
            usuario.IdentificadorFuncionario == identificadorFuncionario
            || usuario.NormalizedUserName == identificadorNormalizado);

        if (identificadorEmUso)
        {
            return BadRequest(new { mensagem = "O identificador deste convite já está em uso." });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        if (!await _roleManager.RoleExistsAsync(convite.Perfil))
        {
            var roleResult = await _roleManager.CreateAsync(new IdentityRole(convite.Perfil));

            if (!roleResult.Succeeded)
            {
                return BadRequest(new
                {
                    mensagem = "Não foi possível preparar o perfil do usuário.",
                    erros = roleResult.Errors.Select(error => error.Description)
                });
            }
        }

        var usuario = new ApplicationUser
        {
            NomeCompleto = convite.NomeCompleto.Trim(),
            Email = convite.Email.Trim(),
            UserName = identificadorFuncionario,
            IdentificadorFuncionario = identificadorFuncionario,
            Perfil = convite.Perfil,
            EmailConfirmed = true,
            Ativo = true,
            DoisFatoresObrigatorio = PerfilExigeDoisFatores(convite.Perfil)
        };

        var createResult = await _userManager.CreateAsync(usuario, request.Senha);

        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Não foi possível cadastrar o funcionário.",
                erros = createResult.Errors.Select(error => error.Description)
            });
        }

        var roleAssignResult = await _userManager.AddToRoleAsync(usuario, convite.Perfil);

        if (!roleAssignResult.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Não foi possível vincular o perfil ao funcionário.",
                erros = roleAssignResult.Errors.Select(error => error.Description)
            });
        }

        convite.Usado = true;
        convite.UsadoEm = DateTime.UtcNow;
        convite.UsuarioId = usuario.Id;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            mensagem = "Funcionário cadastrado com sucesso.",
            identificadorFuncionario = usuario.IdentificadorFuncionario
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var usuario = await EncontrarUsuarioParaLogin(request);

        if (usuario is null || !usuario.Ativo)
        {
            if (usuario is not null && !usuario.Ativo)
            {
                await _auditoriaService.RegistrarAsync(
                    "LOGIN_BLOQUEADO",
                    "ApplicationUser",
                    usuario.Id,
                    $"Tentativa de login bloqueada para usuário inativo {usuario.IdentificadorFuncionario}.");

                return Unauthorized(new { mensagem = "Usuário desativado. Procure a coordenação." });
            }

            await _auditoriaService.RegistrarAsync(
                "LOGIN_FALHA",
                "ApplicationUser",
                null,
                "Tentativa de login falhou para identificador não encontrado ou inválido.");

            return Unauthorized(new { mensagem = "Identificador ou senha inválidos." });
        }

        if (await _userManager.IsLockedOutAsync(usuario))
        {
            await _auditoriaService.RegistrarAsync(
                "LOGIN_BLOQUEADO",
                "ApplicationUser",
                usuario.Id,
                $"Tentativa de login bloqueada temporariamente para {usuario.IdentificadorFuncionario}.");

            return Unauthorized(new { mensagem = "Acesso temporariamente bloqueado. Aguarde alguns minutos e tente novamente." });
        }

        var senhaValida = await _userManager.CheckPasswordAsync(usuario, request.Senha);

        if (!senhaValida)
        {
            await _userManager.AccessFailedAsync(usuario);

            if (await _userManager.IsLockedOutAsync(usuario))
            {
                await _auditoriaService.RegistrarAsync(
                    "LOGIN_BLOQUEADO",
                    "ApplicationUser",
                    usuario.Id,
                    $"Login bloqueado temporariamente após tentativas inválidas para {usuario.IdentificadorFuncionario}.");

                return Unauthorized(new { mensagem = "Acesso temporariamente bloqueado. Aguarde alguns minutos e tente novamente." });
            }

            await _auditoriaService.RegistrarAsync(
                "LOGIN_FALHA",
                "ApplicationUser",
                usuario.Id,
                $"Tentativa de login falhou para {usuario.IdentificadorFuncionario}.");

            return Unauthorized(new { mensagem = "Identificador ou senha inválidos." });
        }

        if (await _userManager.GetAccessFailedCountAsync(usuario) > 0)
        {
            await _userManager.ResetAccessFailedCountAsync(usuario);
        }

        var roles = await _userManager.GetRolesAsync(usuario);

        if (usuario.TwoFactorEnabled)
        {
            return Ok(GerarRespostaDoisFatores(usuario, roles));
        }

        return Ok(GerarAuthResponse(usuario, roles));
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.RedefinirSenha)]
    [HttpPost("redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha(RedefinirSenhaRequest request)
    {
        if (request.NovaSenha != request.ConfirmarNovaSenha)
        {
            await _auditoriaService.RegistrarAsync(
                "REDEFINICAO_SENHA_FALHA",
                "ApplicationUser",
                null,
                "Tentativa de redefinição de senha falhou por confirmação divergente.");

            return BadRequest(new { mensagem = "Nova senha e confirmação não conferem." });
        }

        var email = request.Email.Trim();
        var usuario = await _userManager.FindByEmailAsync(email);

        if (usuario is null || !usuario.Ativo)
        {
            await _auditoriaService.RegistrarAsync(
                "REDEFINICAO_SENHA_FALHA",
                "ApplicationUser",
                usuario?.Id,
                "Tentativa de redefinição de senha inválida para e-mail informado.");

            return BadRequest(new { mensagem = "Solicitação de redefinição inválida." });
        }

        var result = await _userManager.ResetPasswordAsync(usuario, request.Token, request.NovaSenha);

        if (!result.Succeeded)
        {
            await _auditoriaService.RegistrarAsync(
                "REDEFINICAO_SENHA_FALHA",
                "ApplicationUser",
                usuario.Id,
                $"Tentativa de redefinição de senha falhou para {usuario.IdentificadorFuncionario}.");

            return BadRequest(new
            {
                mensagem = "Não foi possível redefinir a senha.",
                erros = result.Errors.Select(error => error.Description)
            });
        }

        usuario.DeveTrocarSenha = false;
        await _userManager.UpdateAsync(usuario);
        await _auditoriaService.RegistrarAsync(
            "REDEFINICAO_SENHA_CONCLUIDA",
            "ApplicationUser",
            usuario.Id,
            $"Funcionário {usuario.IdentificadorFuncionario} concluiu redefinição de senha.");

        return Ok(new { mensagem = "Senha redefinida com sucesso." });
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.SolicitarRedefinicaoSenha)]
    [HttpPost("solicitar-redefinicao-senha")]
    public async Task<IActionResult> SolicitarRedefinicaoSenha(SolicitarRedefinicaoSenhaRequest request)
    {
        const string mensagemGenerica = "Se os dados estiverem corretos, enviaremos as instruções para o e-mail cadastrado.";
        var identificador = request.IdentificadorFuncionario.Trim();

        if (string.IsNullOrWhiteSpace(identificador))
        {
            return Ok(new { mensagem = mensagemGenerica });
        }

        var identificadorNormalizado = identificador.ToUpperInvariant();
        var usuario = await _dbContext.Users.SingleOrDefaultAsync(item =>
            item.NormalizedUserName == identificadorNormalizado
            || item.IdentificadorFuncionario.ToUpper() == identificadorNormalizado);

        if (usuario is null || !usuario.Ativo || string.IsNullOrWhiteSpace(usuario.Email))
        {
            return Ok(new { mensagem = mensagemGenerica });
        }

        if (!_redefinicaoSenhaThrottleService.PermitirSolicitacao(
            usuario.Id,
            ObterIpOrigem(),
            out var motivoBloqueio,
            out var bloqueadoAte))
        {
            await _auditoriaService.RegistrarAsync(
                "REDEFINICAO_SENHA_ABUSO_BLOQUEADO",
                "ApplicationUser",
                usuario.Id,
                $"Solicitação pública de redefinição bloqueada para {usuario.IdentificadorFuncionario}. Motivo: {motivoBloqueio}. Bloqueado até {bloqueadoAte:O}.");

            return Ok(new { mensagem = mensagemGenerica });
        }

        var resultadoEmail = await _redefinicaoSenhaEmailService.EnviarAsync(usuario);
        await _auditoriaService.RegistrarAsync(
            "REDEFINICAO_SENHA_AUTO_SOLICITADA",
            "ApplicationUser",
            usuario.Id,
            $"Solicitação pública de redefinição de senha para {usuario.IdentificadorFuncionario}. Status do e-mail: {resultadoEmail.StatusEmail ?? "Não informado"}.");

        return Ok(new { mensagem = mensagemGenerica });
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.LoginDoisFatores)]
    [HttpPost("login-2fa")]
    public async Task<ActionResult<AuthResponse>> LoginDoisFatores(LoginDoisFatoresRequest request)
    {
        var usuario = await ObterUsuarioDoLoginTemporario(request.LoginTemporario);

        if (usuario is null || !usuario.Ativo || !usuario.TwoFactorEnabled)
        {
            await _auditoriaService.RegistrarAsync(
                "LOGIN_2FA_FALHA",
                "ApplicationUser",
                usuario?.Id,
                "Tentativa de login com código de segurança falhou por login temporário inválido, expirado ou indisponível.");

            return Unauthorized(new { mensagem = "Login temporário inválido ou expirado." });
        }

        if (await _userManager.IsLockedOutAsync(usuario))
        {
            await _auditoriaService.RegistrarAsync(
                "LOGIN_BLOQUEADO",
                "ApplicationUser",
                usuario.Id,
                $"Tentativa de 2FA bloqueada temporariamente para {usuario.IdentificadorFuncionario}.");

            return Unauthorized(new { mensagem = "Acesso temporariamente bloqueado. Aguarde alguns minutos e tente novamente." });
        }

        var codigo = NormalizarCodigoDoisFatores(request.Codigo);
        var valido = await _userManager.VerifyTwoFactorTokenAsync(
            usuario,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            codigo);

        if (!valido)
        {
            await _userManager.AccessFailedAsync(usuario);

            if (await _userManager.IsLockedOutAsync(usuario))
            {
                await _auditoriaService.RegistrarAsync(
                    "LOGIN_BLOQUEADO",
                    "ApplicationUser",
                    usuario.Id,
                    $"Login bloqueado temporariamente após falhas de 2FA para {usuario.IdentificadorFuncionario}.");

                return Unauthorized(new { mensagem = "Acesso temporariamente bloqueado. Aguarde alguns minutos e tente novamente." });
            }

            await _auditoriaService.RegistrarAsync(
                "LOGIN_2FA_FALHA",
                "ApplicationUser",
                usuario.Id,
                $"Tentativa de login com código de segurança falhou para {usuario.IdentificadorFuncionario}.");

            return Unauthorized(new { mensagem = "Código de segurança inválido." });
        }

        if (await _userManager.GetAccessFailedCountAsync(usuario) > 0)
        {
            await _userManager.ResetAccessFailedCountAsync(usuario);
        }

        var roles = await _userManager.GetRolesAsync(usuario);
        return Ok(GerarAuthResponse(usuario, roles));
    }

    [Authorize]
    [HttpPost("2fa/iniciar-configuracao")]
    public async Task<ActionResult<DoisFatoresConfiguracaoResponse>> IniciarConfiguracaoDoisFatores()
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        if (usuario.TwoFactorEnabled)
        {
            return BadRequest(new { mensagem = "O código de segurança já está ativo para este usuário." });
        }

        await _userManager.ResetAuthenticatorKeyAsync(usuario);
        var chave = await _userManager.GetAuthenticatorKeyAsync(usuario);

        if (string.IsNullOrWhiteSpace(chave))
        {
            return BadRequest(new { mensagem = "Não foi possível iniciar a configuração do aplicativo autenticador." });
        }

        var uri = GerarAuthenticatorUri(usuario, chave);

        return Ok(new DoisFatoresConfiguracaoResponse
        {
            Mensagem = "Configuração iniciada com sucesso.",
            ChaveManual = FormatarChaveManual(chave),
            AuthenticatorUri = uri,
            QrCodeData = uri
        });
    }

    [Authorize]
    [HttpPost("2fa/confirmar")]
    public async Task<IActionResult> ConfirmarDoisFatores(ConfirmarDoisFatoresRequest request)
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        var codigo = NormalizarCodigoDoisFatores(request.Codigo);
        var valido = await _userManager.VerifyTwoFactorTokenAsync(
            usuario,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            codigo);

        if (!valido)
        {
            return BadRequest(new { mensagem = "Código de segurança inválido." });
        }

        await _userManager.SetTwoFactorEnabledAsync(usuario, true);

        return Ok(new { mensagem = "Código de segurança ativado com sucesso." });
    }

    [Authorize]
    [HttpPost("2fa/desativar")]
    public async Task<IActionResult> DesativarDoisFatores()
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        if (usuario.DoisFatoresObrigatorio)
        {
            return BadRequest(new { mensagem = "O código de segurança é obrigatório para este perfil." });
        }

        await _userManager.SetTwoFactorEnabledAsync(usuario, false);
        await _userManager.ResetAuthenticatorKeyAsync(usuario);

        return Ok(new { mensagem = "Código de segurança desativado." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UsuarioAtualResponse>> Me()
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        return Ok(new UsuarioAtualResponse
        {
            NomeCompleto = usuario.NomeCompleto,
            Email = usuario.Email ?? string.Empty,
            EmailRecuperacao = usuario.EmailRecuperacao,
            EmailRecuperacaoConfirmado = usuario.EmailRecuperacaoConfirmado,
            Perfil = usuario.Perfil,
            IdentificadorFuncionario = usuario.IdentificadorFuncionario,
            DoisFatoresObrigatorio = usuario.DoisFatoresObrigatorio,
            DoisFatoresAtivado = usuario.TwoFactorEnabled,
            DeveTrocarSenha = usuario.DeveTrocarSenha
        });
    }

    [Authorize]
    [HttpPost("email-recuperacao/solicitar")]
    public async Task<ActionResult<EmailRecuperacaoResponse>> SolicitarEmailRecuperacao(SolicitarEmailRecuperacaoRequest request)
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        var emailRecuperacao = request.EmailRecuperacao.Trim();

        if (string.IsNullOrWhiteSpace(emailRecuperacao))
        {
            return BadRequest(new { mensagem = "Informe um e-mail de recuperação válido." });
        }

        if (string.Equals(usuario.Email, emailRecuperacao, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { mensagem = "O e-mail de recuperação deve ser diferente do e-mail principal." });
        }

        if (await EmailRecuperacaoEstaEmUsoPorOutroUsuarioAsync(usuario.Id, emailRecuperacao))
        {
            return BadRequest(new { mensagem = "Este e-mail não pode ser usado como e-mail de recuperação." });
        }

        if (usuario.EmailRecuperacaoConfirmado
            && string.Equals(usuario.EmailRecuperacao, emailRecuperacao, StringComparison.OrdinalIgnoreCase))
        {
            return Ok(MapearEmailRecuperacaoResponse(
                usuario,
                "Este e-mail de recuperação já está confirmado.",
                null));
        }

        usuario.EmailRecuperacao = emailRecuperacao;
        usuario.EmailRecuperacaoConfirmado = false;
        usuario.EmailRecuperacaoConfirmadoEm = null;

        var updateResult = await _userManager.UpdateAsync(usuario);

        if (!updateResult.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Não foi possível salvar o e-mail de recuperação.",
                erros = updateResult.Errors.Select(error => error.Description)
            });
        }

        var resultadoEmail = await _emailRecuperacaoEmailService.EnviarConfirmacaoAsync(usuario);

        await _auditoriaService.RegistrarAsync(
            "EMAIL_RECUPERACAO_SOLICITADO",
            "ApplicationUser",
            usuario.Id,
            $"Solicitou confirmação de e-mail de recuperação para {MascararEmail(emailRecuperacao)}. Status do e-mail: {resultadoEmail.StatusEmail ?? "Não informado"}.");

        return Ok(MapearEmailRecuperacaoResponse(
            usuario,
            resultadoEmail.EmailEnviado
                ? "Enviamos um link de confirmação para o e-mail informado."
                : resultadoEmail.AvisoEmail ?? "Não foi possível enviar o link de confirmação.",
            resultadoEmail));
    }

    [AllowAnonymous]
    [HttpPost("email-recuperacao/confirmar")]
    public async Task<ActionResult<EmailRecuperacaoResponse>> ConfirmarEmailRecuperacao(ConfirmarEmailRecuperacaoRequest request)
    {
        var emailRecuperacao = request.EmailRecuperacao.Trim();

        if (string.IsNullOrWhiteSpace(emailRecuperacao) || string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { mensagem = "Solicitação de confirmação inválida." });
        }

        var emailNormalizado = emailRecuperacao.ToUpperInvariant();
        var usuario = await _dbContext.Users.FirstOrDefaultAsync(item =>
            item.EmailRecuperacao != null
            && item.EmailRecuperacao.ToUpper() == emailNormalizado);

        if (usuario is null || !usuario.Ativo)
        {
            return BadRequest(new { mensagem = "Solicitação de confirmação inválida ou expirada." });
        }

        var tokenValido = await _userManager.VerifyUserTokenAsync(
            usuario,
            TokenOptions.DefaultProvider,
            EmailRecuperacaoTokenPurpose.Criar(emailRecuperacao),
            request.Token);

        if (!tokenValido)
        {
            await _auditoriaService.RegistrarAsync(
                "EMAIL_RECUPERACAO_CONFIRMACAO_FALHA",
                "ApplicationUser",
                usuario.Id,
                $"Confirmação de e-mail de recuperação falhou para {usuario.IdentificadorFuncionario}. Token não registrado.");

            return BadRequest(new { mensagem = "Solicitação de confirmação inválida ou expirada." });
        }

        usuario.EmailRecuperacao = emailRecuperacao;
        usuario.EmailRecuperacaoConfirmado = true;
        usuario.EmailRecuperacaoConfirmadoEm = DateTime.UtcNow;

        await _userManager.UpdateAsync(usuario);
        await _auditoriaService.RegistrarAsync(
            "EMAIL_RECUPERACAO_CONFIRMADO",
            "ApplicationUser",
            usuario.Id,
            $"E-mail de recuperação confirmado para {usuario.IdentificadorFuncionario}.");

        return Ok(MapearEmailRecuperacaoResponse(
            usuario,
            "E-mail de recuperação confirmado com sucesso.",
            null));
    }

    [Authorize]
    [HttpDelete("email-recuperacao")]
    public async Task<IActionResult> RemoverEmailRecuperacao()
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(usuario.EmailRecuperacao))
        {
            return Ok(new { mensagem = "Nenhum e-mail de recuperação cadastrado." });
        }

        usuario.EmailRecuperacao = null;
        usuario.EmailRecuperacaoConfirmado = false;
        usuario.EmailRecuperacaoConfirmadoEm = null;

        await _userManager.UpdateAsync(usuario);
        await _auditoriaService.RegistrarAsync(
            "EMAIL_RECUPERACAO_REMOVIDO",
            "ApplicationUser",
            usuario.Id,
            $"Removeu o e-mail de recuperação de {usuario.IdentificadorFuncionario}.");

        return Ok(new { mensagem = "E-mail de recuperação removido." });
    }

    [Authorize]
    [HttpPost("trocar-senha-obrigatoria")]
    public async Task<IActionResult> TrocarSenhaObrigatoria(TrocarSenhaObrigatoriaRequest request)
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        if (request.NovaSenha != request.ConfirmarNovaSenha)
        {
            return BadRequest(new { mensagem = "Nova senha e confirmação não conferem." });
        }

        var result = await _userManager.ChangePasswordAsync(usuario, request.SenhaAtual, request.NovaSenha);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Não foi possível trocar a senha.",
                erros = result.Errors.Select(error => error.Description)
            });
        }

        usuario.DeveTrocarSenha = false;
        await _userManager.UpdateAsync(usuario);
        await _auditoriaService.RegistrarAsync(
            "SENHA_TROCADA",
            "ApplicationUser",
            usuario.Id,
            $"Funcionário {usuario.IdentificadorFuncionario} concluiu a troca obrigatória de senha.");

        return Ok(new { mensagem = "Senha alterada com sucesso." });
    }

    // ── Passkey login — iniciar ────────────────────────────────────────────

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PasskeyLoginIniciar)]
    [HttpPost("passkey/login/iniciar")]
    public async Task<ActionResult<PasskeyLoginIniciarResponse>> PasskeyLoginIniciar()
    {
        // Busca todas as credenciais cadastradas (sem filtrar por usuário — login discoverable)
        var todasCredenciais = await _dbContext.PasskeyCredentials
            .Select(c => c.CredentialId)
            .ToListAsync();

        if (todasCredenciais.Count == 0)
        {
            return BadRequest(new
            {
                mensagem = "Nenhuma chave de acesso cadastrada. Entre com ID e senha e ative uma chave em Segurança."
            });
        }

        var allowCredentials = todasCredenciais
            .Select(id => new PublicKeyCredentialDescriptor(id))
            .ToList();

        var options = _fido2.GetAssertionOptions(
            allowCredentials,
            UserVerificationRequirement.Required);

        var challengeId = Guid.NewGuid().ToString("N");
        var optionsJson = options.ToJson();

        _dbContext.PasskeyChallenges.Add(new PasskeyChallenge
        {
            ChallengeId = challengeId,
            ChallengeBytes = options.Challenge,
            Tipo = "Login",
            OptionsJson = optionsJson,
            UserId = null,
            CriadoEm = DateTime.UtcNow,
            ExpiracaoEm = DateTime.UtcNow.Add(PasskeyChallengeValidade)
        });

        await _dbContext.SaveChangesAsync();

        return Ok(new PasskeyLoginIniciarResponse
        {
            ChallengeId = challengeId,
            PublicKeyOptions = JsonNode.Parse(optionsJson)
        });
    }

    // ── Passkey login — concluir ───────────────────────────────────────────

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PasskeyLoginConcluir)]
    [HttpPost("passkey/login/concluir")]
    public async Task<ActionResult<PasskeyLoginConcluirResponse>> PasskeyLoginConcluir(PasskeyLoginConcluirRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ChallengeId))
        {
            return BadRequest(new { mensagem = "ChallengeId inválido." });
        }

        var challenge = await _dbContext.PasskeyChallenges
            .SingleOrDefaultAsync(c => c.ChallengeId == request.ChallengeId && c.Tipo == "Login");

        if (challenge is null || challenge.ExpiracaoEm < DateTime.UtcNow)
        {
            return BadRequest(new { mensagem = "Sessão de login expirada ou inválida. Tente novamente." });
        }

        AssertionOptions assertionOptions;

        try
        {
            assertionOptions = AssertionOptions.FromJson(challenge.OptionsJson);
        }
        catch
        {
            return BadRequest(new { mensagem = "Não foi possível recuperar o contexto de login." });
        }

        if (request.Credential is null)
        {
            return BadRequest(new { mensagem = "Credencial não informada." });
        }

        AuthenticatorAssertionRawResponse assertionResponse;

        try
        {
            var credJson = request.Credential.ToJsonString();
            assertionResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(credJson)
                ?? throw new InvalidOperationException("Deserialização retornou null.");
        }
        catch
        {
            return BadRequest(new { mensagem = "Formato da credencial inválido." });
        }

        // Encontrar a credencial pelo rawId enviado pelo browser
        var rawId = assertionResponse.RawId;
        var credencial = await _dbContext.PasskeyCredentials
            .Include(c => c.User)
            .SingleOrDefaultAsync(c => c.CredentialId == rawId);

        if (credencial is null || credencial.User is null)
        {
            await _auditoriaService.RegistrarAsync(
                "PASSKEY_LOGIN_FALHA",
                "PasskeyCredential",
                null,
                "Tentativa de login por passkey com credencial desconhecida.");

            return Unauthorized(new { mensagem = "Chave de acesso não reconhecida." });
        }

        var usuario = credencial.User;

        if (!usuario.Ativo)
        {
            await _auditoriaService.RegistrarAsync(
                "PASSKEY_LOGIN_FALHA",
                "PasskeyCredential",
                usuario.Id,
                $"Login por passkey bloqueado para usuário inativo {usuario.IdentificadorFuncionario}.");

            return Unauthorized(new { mensagem = "Usuário desativado. Procure a coordenação." });
        }

        // Validar assinatura com Fido2NetLib
        IsUserHandleOwnerOfCredentialIdAsync isUserHandleOwner = (args, _) =>
            Task.FromResult(args.UserHandle.SequenceEqual(System.Text.Encoding.UTF8.GetBytes(usuario.Id)));

        AssertionVerificationResult assertionResult;

        try
        {
            assertionResult = await _fido2.MakeAssertionAsync(
                assertionResponse,
                assertionOptions,
                credencial.PublicKey,
                credencial.SignatureCounter,
                isUserHandleOwner);
        }
        catch (Fido2VerificationException ex)
        {
            await _auditoriaService.RegistrarAsync(
                "PASSKEY_LOGIN_FALHA",
                "PasskeyCredential",
                usuario.Id,
                $"Falha na validação da assinatura passkey para {usuario.IdentificadorFuncionario}: {ex.Message}");

            return Unauthorized(new { mensagem = "Falha na verificação da chave de acesso." });
        }

        // Atualizar contador e último uso
        credencial.SignatureCounter = assertionResult.Counter;
        credencial.UltimoUsoEm = DateTime.UtcNow;
        _dbContext.PasskeyChallenges.Remove(challenge);
        await _dbContext.SaveChangesAsync();

        // Verificar regra dos 7 dias
        var primeiroAcessoPorPasskey = usuario.PasskeyReconfirmadoEm is null;
        var precisaReconfirmar = primeiroAcessoPorPasskey
            || DateTime.UtcNow - usuario.PasskeyReconfirmadoEm!.Value > PasskeyReconfirmacaoPrazo;

        if (precisaReconfirmar)
        {
            var motivoReconfirmacao = primeiroAcessoPorPasskey
                ? "primeiro_acesso"
                : "prazo_7_dias";
            var descricaoMotivoReconfirmacao = primeiroAcessoPorPasskey
                ? "primeiro acesso por passkey"
                : "prazo de 7 dias expirado";
            var reconfirmacaoId = Guid.NewGuid().ToString("N");

            _dbContext.PasskeyReconfirmacoes.Add(new PasskeyReconfirmacao
            {
                ReconfirmacaoId = reconfirmacaoId,
                UserId = usuario.Id,
                CredentialId = credencial.CredentialId,
                CriadoEm = DateTime.UtcNow,
                ExpiracaoEm = DateTime.UtcNow.Add(PasskeyReconfirmacaoValidade)
            });

            await _dbContext.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync(
                "PASSKEY_RECONFIRMACAO_SOLICITADA",
                "PasskeyCredential",
                usuario.Id,
                $"Reconfirmação de credenciais solicitada para {usuario.IdentificadorFuncionario} ({descricaoMotivoReconfirmacao}).");

            var roles = await _userManager.GetRolesAsync(usuario);

            return Ok(new PasskeyLoginConcluirResponse
            {
                RequerReconfirmacao = true,
                MotivoReconfirmacao = motivoReconfirmacao,
                ReconfirmacaoId = reconfirmacaoId,
                NomeCompleto = usuario.NomeCompleto,
                Email = usuario.Email ?? string.Empty,
                Perfil = roles.FirstOrDefault() ?? usuario.Perfil,
                IdentificadorFuncionario = usuario.IdentificadorFuncionario,
                DoisFatoresObrigatorio = usuario.DoisFatoresObrigatorio,
                DoisFatoresAtivado = usuario.TwoFactorEnabled,
                TemDoisFatores = usuario.TwoFactorEnabled,
                DeveTrocarSenha = usuario.DeveTrocarSenha
            });
        }

        var rolesLogin = await _userManager.GetRolesAsync(usuario);

        await _auditoriaService.RegistrarAsync(
            "PASSKEY_LOGIN_SUCESSO",
            "PasskeyCredential",
            usuario.Id,
            $"Login por passkey concluído para {usuario.IdentificadorFuncionario}.");

        var jwtLoginPasskey = GerarJwt(usuario, rolesLogin);

        return Ok(new PasskeyLoginConcluirResponse
        {
            Token = jwtLoginPasskey.Token,
            ExpiraEm = jwtLoginPasskey.ExpiraEm,
            NomeCompleto = usuario.NomeCompleto,
            Email = usuario.Email ?? string.Empty,
            Perfil = rolesLogin.FirstOrDefault() ?? usuario.Perfil,
            IdentificadorFuncionario = usuario.IdentificadorFuncionario,
            DoisFatoresObrigatorio = usuario.DoisFatoresObrigatorio,
            DoisFatoresAtivado = usuario.TwoFactorEnabled,
            TemDoisFatores = usuario.TwoFactorEnabled,
            DeveTrocarSenha = usuario.DeveTrocarSenha
        });
    }

    // ── Passkey — reconfirmação dos 7 dias ────────────────────────────────

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PasskeyReconfirmar)]
    [HttpPost("passkey/reconfirmar")]
    public async Task<ActionResult<PasskeyLoginConcluirResponse>> PasskeyReconfirmar(PasskeyReconfirmarRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReconfirmacaoId))
        {
            return BadRequest(new { mensagem = "Token de reconfirmação inválido." });
        }

        var reconfirmacao = await _dbContext.PasskeyReconfirmacoes
            .SingleOrDefaultAsync(r => r.ReconfirmacaoId == request.ReconfirmacaoId);

        if (reconfirmacao is null || reconfirmacao.ExpiracaoEm < DateTime.UtcNow)
        {
            return Unauthorized(new { mensagem = "Token de reconfirmação expirado ou inválido. Faça login por passkey novamente." });
        }

        // Verificar ID + senha
        var requestComIdentificador = new LoginRequest
        {
            Identificador = request.IdentificadorFuncionario,
            Email = string.Empty,
            Senha = request.Senha
        };

        var usuario = await EncontrarUsuarioParaLogin(requestComIdentificador);

        if (usuario is null || !usuario.Ativo || usuario.Id != reconfirmacao.UserId)
        {
            await _auditoriaService.RegistrarAsync(
                "PASSKEY_RECONFIRMACAO_FALHA",
                "PasskeyCredential",
                reconfirmacao.UserId,
                "Reconfirmação de passkey falhou: usuário não localizado ou inativo.");

            return Unauthorized(new { mensagem = "Identificador ou senha inválidos." });
        }

        if (await _userManager.IsLockedOutAsync(usuario))
        {
            return Unauthorized(new { mensagem = "Acesso temporariamente bloqueado. Aguarde alguns minutos e tente novamente." });
        }

        var senhaValida = await _userManager.CheckPasswordAsync(usuario, request.Senha);

        if (!senhaValida)
        {
            await _userManager.AccessFailedAsync(usuario);

            await _auditoriaService.RegistrarAsync(
                "PASSKEY_RECONFIRMACAO_FALHA",
                "PasskeyCredential",
                usuario.Id,
                $"Reconfirmação de passkey falhou por senha incorreta para {usuario.IdentificadorFuncionario}.");

            return Unauthorized(new { mensagem = "Identificador ou senha inválidos." });
        }

        // Se o usuário tem 2FA ativo, exigir código do aplicativo
        if (usuario.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.CodigoDoAplicativo))
            {
                return BadRequest(new { mensagem = "Informe o código do aplicativo autenticador." });
            }

            var codigo = NormalizarCodigoDoisFatores(request.CodigoDoAplicativo);
            var codigoValido = await _userManager.VerifyTwoFactorTokenAsync(
                usuario,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                codigo);

            if (!codigoValido)
            {
                await _userManager.AccessFailedAsync(usuario);

                await _auditoriaService.RegistrarAsync(
                    "PASSKEY_RECONFIRMACAO_FALHA",
                    "PasskeyCredential",
                    usuario.Id,
                    $"Reconfirmação de passkey falhou por código autenticador incorreto para {usuario.IdentificadorFuncionario}.");

                return Unauthorized(new { mensagem = "Código de segurança inválido." });
            }
        }

        if (await _userManager.GetAccessFailedCountAsync(usuario) > 0)
        {
            await _userManager.ResetAccessFailedCountAsync(usuario);
        }

        // Atualizar data de reconfirmação e remover token temporário
        usuario.PasskeyReconfirmadoEm = DateTime.UtcNow;
        await _userManager.UpdateAsync(usuario);

        _dbContext.PasskeyReconfirmacoes.Remove(reconfirmacao);
        await _dbContext.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(usuario);

        await _auditoriaService.RegistrarAsync(
            "PASSKEY_RECONFIRMADA",
            "PasskeyCredential",
            usuario.Id,
            $"Credenciais reconfirmadas com sucesso para login por passkey de {usuario.IdentificadorFuncionario}.");

        var jwtReconfirmacao = GerarJwt(usuario, roles);

        return Ok(new PasskeyLoginConcluirResponse
        {
            Token = jwtReconfirmacao.Token,
            ExpiraEm = jwtReconfirmacao.ExpiraEm,
            NomeCompleto = usuario.NomeCompleto,
            Email = usuario.Email ?? string.Empty,
            Perfil = roles.FirstOrDefault() ?? usuario.Perfil,
            IdentificadorFuncionario = usuario.IdentificadorFuncionario,
            DoisFatoresObrigatorio = usuario.DoisFatoresObrigatorio,
            DoisFatoresAtivado = usuario.TwoFactorEnabled,
            TemDoisFatores = usuario.TwoFactorEnabled,
            DeveTrocarSenha = usuario.DeveTrocarSenha
        });
    }

    private Task RegistrarConvitePublicoInvalidoAsync(string descricao)
    {
        return _auditoriaService.RegistrarAsync(
            "CONVITE_PUBLICO_INVALIDO",
            "FuncionarioConvite",
            null,
            descricao);
    }

    private async Task<bool> EmailRecuperacaoEstaEmUsoPorOutroUsuarioAsync(string usuarioId, string emailRecuperacao)
    {
        var emailNormalizado = emailRecuperacao.Trim().ToUpperInvariant();

        return await _dbContext.Users.AnyAsync(usuario =>
            usuario.Id != usuarioId
            && (
                usuario.NormalizedEmail == emailNormalizado
                || (
                    usuario.EmailRecuperacao != null
                    && usuario.EmailRecuperacao.ToUpper() == emailNormalizado
                )
            ));
    }

    private static EmailRecuperacaoResponse MapearEmailRecuperacaoResponse(
        ApplicationUser usuario,
        string mensagem,
        ResultadoEmailRecuperacao? resultadoEmail)
    {
        return new EmailRecuperacaoResponse
        {
            Mensagem = mensagem,
            EmailRecuperacao = usuario.EmailRecuperacao,
            EmailRecuperacaoConfirmado = usuario.EmailRecuperacaoConfirmado,
            EmailRecuperacaoConfirmadoEm = usuario.EmailRecuperacaoConfirmadoEm,
            StatusEmail = resultadoEmail?.StatusEmail,
            AvisoEmail = resultadoEmail?.AvisoEmail,
            LinkConfirmacaoDesenvolvimento = resultadoEmail?.LinkConfirmacaoDesenvolvimento
        };
    }

    private static string MascararEmail(string email)
    {
        var partes = email.Split('@', 2);

        if (partes.Length != 2 || partes[0].Length <= 2)
        {
            return "***";
        }

        return $"{partes[0][0]}***{partes[0][^1]}@{partes[1]}";
    }

    private string ObterIpOrigem()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido";
    }

    private async Task<ApplicationUser?> EncontrarUsuarioParaLogin(LoginRequest request)
    {
        var identificador = request.Identificador.Trim();

        if (string.IsNullOrWhiteSpace(identificador))
        {
            identificador = request.Email.Trim();
        }

        if (string.IsNullOrWhiteSpace(identificador))
        {
            return null;
        }

        if (identificador.Contains('@'))
        {
            return await _userManager.FindByEmailAsync(identificador);
        }

        var identificadorNormalizado = identificador.ToUpperInvariant();

        return await _dbContext.Users.SingleOrDefaultAsync(usuario =>
            usuario.NormalizedUserName == identificadorNormalizado
            || usuario.IdentificadorFuncionario.ToUpper() == identificadorNormalizado);
    }

    private async Task<FuncionarioConvite?> ObterConvitePorCodigoAsync(string codigoCadastro)
    {
        if (string.IsNullOrWhiteSpace(codigoCadastro))
        {
            return null;
        }

        var codigo = codigoCadastro.Trim();
        var codigoHash = _codigoService.GerarHash(codigo);
        var convite = await _dbContext.FuncionariosConvites
            .SingleOrDefaultAsync(item => item.CodigoHash == codigoHash);

        if (convite is null || !_codigoService.CodigoCorresponde(codigo, convite.CodigoHash))
        {
            return null;
        }

        return convite;
    }

    private ActionResult? ValidarConviteParaFinalizacao(FuncionarioConvite? convite, string email)
    {
        if (convite is null)
        {
            return BadRequest(new { mensagem = "Convite inválido." });
        }

        if (convite.Cancelado)
        {
            return BadRequest(new { mensagem = "Convite cancelado." });
        }

        if (convite.Usado)
        {
            return BadRequest(new { mensagem = "Convite já utilizado." });
        }

        if (convite.ExpiraEm < DateTime.UtcNow)
        {
            return BadRequest(new { mensagem = "Convite expirado." });
        }

        if (!string.Equals(convite.Email.Trim(), email, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { mensagem = "E-mail informado não corresponde ao convite." });
        }

        if (!PerfisAcesso.EhValido(convite.Perfil))
        {
            return BadRequest(new { mensagem = "Perfil do convite inválido." });
        }

        if (string.IsNullOrWhiteSpace(convite.IdentificadorFuncionario))
        {
            return BadRequest(new { mensagem = "Convite sem identificador de funcionário reservado." });
        }

        return null;
    }

    private static bool PerfilExigeDoisFatores(string perfil)
    {
        return string.Equals(perfil, PerfisAcesso.Adm, StringComparison.OrdinalIgnoreCase)
            || string.Equals(perfil, PerfisAcesso.Juridico, StringComparison.OrdinalIgnoreCase)
            || string.Equals(perfil, PerfisAcesso.AssistenteSocial, StringComparison.OrdinalIgnoreCase);
    }

    private AuthResponse GerarRespostaDoisFatores(ApplicationUser usuario, IEnumerable<string> roles)
    {
        return new AuthResponse
        {
            NomeCompleto = usuario.NomeCompleto,
            Email = usuario.Email ?? string.Empty,
            Perfil = roles.FirstOrDefault() ?? usuario.Perfil,
            IdentificadorFuncionario = usuario.IdentificadorFuncionario,
            RequerDoisFatores = true,
            LoginTemporario = GerarLoginTemporario(usuario),
            DoisFatoresObrigatorio = usuario.DoisFatoresObrigatorio,
            DoisFatoresAtivado = usuario.TwoFactorEnabled,
            DeveTrocarSenha = usuario.DeveTrocarSenha
        };
    }

    private AuthResponse GerarAuthResponse(ApplicationUser usuario, IEnumerable<string> roles)
    {
        var jwt = GerarJwt(usuario, roles);

        return new AuthResponse
        {
            Token = jwt.Token,
            ExpiraEm = jwt.ExpiraEm,
            NomeCompleto = usuario.NomeCompleto,
            Email = usuario.Email ?? string.Empty,
            Perfil = roles.FirstOrDefault() ?? usuario.Perfil,
            IdentificadorFuncionario = usuario.IdentificadorFuncionario,
            RequerDoisFatores = false,
            DoisFatoresObrigatorio = usuario.DoisFatoresObrigatorio,
            DoisFatoresAtivado = usuario.TwoFactorEnabled,
            DeveTrocarSenha = usuario.DeveTrocarSenha
        };
    }

    private JwtEmitido GerarJwt(ApplicationUser usuario, IEnumerable<string> roles)
    {
        var key = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Configure Jwt:Key para gerar tokens.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id),
            new(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id),
            new(ClaimTypes.Name, usuario.NomeCompleto),
            new("perfil", usuario.Perfil),
            new("identificadorFuncionario", usuario.IdentificadorFuncionario)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expirationHours = _configuration.GetValue("Jwt:ExpirationHours", 24);
        var expiraEm = DateTime.UtcNow.AddHours(expirationHours);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiraEm,
            signingCredentials: credentials);

        return new JwtEmitido(new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }

    private string GerarLoginTemporario(ApplicationUser usuario)
    {
        var ticket = new LoginTemporarioTicket(
            usuario.Id,
            usuario.SecurityStamp ?? string.Empty,
            DateTimeOffset.UtcNow);

        return _loginDoisFatoresProtector.Protect(JsonSerializer.Serialize(ticket));
    }

    private async Task<ApplicationUser?> ObterUsuarioDoLoginTemporario(string loginTemporario)
    {
        try
        {
            var json = _loginDoisFatoresProtector.Unprotect(loginTemporario);
            var ticket = JsonSerializer.Deserialize<LoginTemporarioTicket>(json);

            if (ticket is null || DateTimeOffset.UtcNow - ticket.EmitidoEm > LoginTemporarioValidade)
            {
                return null;
            }

            var usuario = await _userManager.FindByIdAsync(ticket.UsuarioId);

            if (usuario is null || !string.Equals(usuario.SecurityStamp, ticket.SecurityStamp, StringComparison.Ordinal))
            {
                return null;
            }

            return usuario;
        }
        catch
        {
            return null;
        }
    }

    private async Task<ApplicationUser?> ObterUsuarioAtual()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return null;
        }

        return await _userManager.FindByIdAsync(usuarioId);
    }

    private static string NormalizarCodigoDoisFatores(string codigo)
    {
        return codigo.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static string GerarAuthenticatorUri(ApplicationUser usuario, string chave)
    {
        var conta = string.IsNullOrWhiteSpace(usuario.IdentificadorFuncionario)
            ? usuario.Email ?? usuario.Id
            : usuario.IdentificadorFuncionario;

        return "otpauth://totp/"
            + $"{Uri.EscapeDataString(AuthenticatorIssuer)}:{Uri.EscapeDataString(conta)}"
            + $"?secret={Uri.EscapeDataString(chave)}"
            + $"&issuer={Uri.EscapeDataString(AuthenticatorIssuer)}"
            + "&digits=6"
            + "&period=30";
    }

    private static string FormatarChaveManual(string chave)
    {
        return string.Join(" ", chave.Chunk(4).Select(grupo => new string(grupo)));
    }

    private sealed record LoginTemporarioTicket(string UsuarioId, string SecurityStamp, DateTimeOffset EmitidoEm);
}
